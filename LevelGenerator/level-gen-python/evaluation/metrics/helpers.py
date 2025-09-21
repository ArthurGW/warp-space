import heapq
from collections import defaultdict
from dataclasses import dataclass, field
from functools import wraps

from .level import Level, Room


_caches_by_fn = {}


def cache(fn):
    """Caches/memoizes a function's result based on params"""
    _caches_by_fn[fn] = {}
    _sentinel = object()  # Use this as None could be a valid return value

    @wraps(fn)
    def inner(*args, **kwargs):
        cache_key = {k: (v if not isinstance(v, Level) else v.cache_key) for k, v in kwargs.items()}
        cache_key['__!args!__'] = tuple((a if not isinstance(a, Level) else a.cache_key) for a in args)
        if (res := _caches_by_fn[fn].get(str(cache_key), _sentinel)) is _sentinel:
            _caches_by_fn[fn][str(cache_key)] = res = fn(*args, **kwargs)
        return res

    return inner


@dataclass(order=True)
class _HeapEntry:
    key: int
    room: Room = field(compare=False)


def _parent_index(index):
    return int((index - 1) / 2) if index % 2 else int((index - 2) / 2)


def _maybe_decrease_key(queue: list[_HeapEntry], entry: _HeapEntry, new_key: int):
    if new_key >= entry.key:
        return False

    entry.key = new_key
    index = next(i for i, e in enumerate(queue) if e is entry)
    if index == 0:
        return True  # Already minimum

    parent_index = _parent_index(index)
    while parent_index >= 0 and queue[index].key < queue[parent_index].key:
        queue[index], queue[parent_index] = queue[parent_index], queue[index]
        index = parent_index
        parent_index = _parent_index(index)

    return True


@cache
def _dijkstra_algorithm(level: Level, start_room: Room, finish_room: Room | None = None) -> tuple[dict[Room, int], dict[Room, list[Room]]]:
    """Runs Dijkstra's algorithm over a level"""
    all_rooms = level.rooms

    distances = defaultdict(lambda: -1)  # -1 means "unreachable" (or not yet reached, if finish_room is given)
    distances[start_room] = 0
    paths = defaultdict(lambda: [])  # empty means "unreachable"
    previous: dict[Room, Room | None] = defaultdict(lambda: None)   # None means "unreachable"

    heap_entries_by_room = {start_room: _HeapEntry(0, start_room)}
    heap_entries_by_room.update(
        {room: _HeapEntry(level.num_rooms + 1, room) for room in all_rooms if room is not start_room}
    )

    queue = list(heap_entries_by_room.values())
    while queue:
        entry = heapq.heappop(queue)
        room = entry.room
        distance = entry.key

        distances[room] = distance

        if finish_room is not None and room is finish_room:
            break

        for connection in level.get_connections_for_room(room):
            if _maybe_decrease_key(queue, heap_entries_by_room[connection], distance + 1):
                previous[connection] = room

    for room in distances:
        path = [room]
        prev = room
        while (prev := previous[prev]) not in {start_room, None}:
            path.append(prev)
        if path[-1] is not start_room:
            path.append(start_room)
        paths[room] = list(reversed(path))

    return distances, paths


def dijkstra_distance(level: Level, start_room: Room, finish_room: Room | None = None) -> dict[Room, int]:
    """Measures the distance from start_room to each room in a level, optionally stopping when finish_room is reached,
    if it is given

    Uses Dijkstra's algorithm."""
    return _dijkstra_algorithm(level, start_room, finish_room)[0]


def dijkstra_path(level: Level, start_room: Room, finish_room: Room | None = None) -> dict[Room, list[Room]]:
    """Finds the path from start_room to each room in a level, optionally stopping when finish_room is reached, if it is
    given

    Uses Dijkstra's algorithm."""
    return _dijkstra_algorithm(level, start_room, finish_room)[1]
