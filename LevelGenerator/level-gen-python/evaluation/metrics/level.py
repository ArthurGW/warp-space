from dataclasses import dataclass
from collections import defaultdict


@dataclass(frozen=True)
class Room:
    x: int
    y: int
    w: int
    h: int


class Level:

    def __init__(self, total_size: int, allow_duplicate_connections=False):
        self._rooms: set[Room] = set()
        self._dangerous_rooms: set[Room] = set()
        self._connections: dict[Room, list[Room] | set[Room]] = defaultdict(
            list if allow_duplicate_connections else set
        )
        self._start_room: Room | None = None
        self._finish_room: Room | None = None
        self._total_size: int = total_size
        self._cache_key = 0  # Simple tracker of updates to level

    @property
    def cache_key(self):
        return id(self), self._cache_key

    @property
    def start_room(self):
        return self._start_room

    @property
    def finish_room(self):
        return self._finish_room

    @property
    def total_size(self):
        return self._total_size

    @property
    def rooms(self):
        return frozenset(self._rooms)

    @property
    def num_rooms(self):
        return len(self._rooms)

    @property
    def dangerous_rooms(self):
        return frozenset(self._dangerous_rooms)

    def get_connections_for_room(self, room: Room):
        return list(self._connections[room])

    def add_room(self, room: Room):
        if room not in self._rooms:
            self._rooms.add(room)
            self._cache_key += 1

    def _add_connection(self, conns: list[Room] | set[Room], to: Room):
        match conns:
            case list():
                conns.append(to)
                self._cache_key += 1
            case set():
                old = len(conns)
                conns.add(to)
                if old != len(conns):
                    self._cache_key += 1

    def add_connection(self, first: Room, second: Room):
        self._add_connection(self._connections[first], second)
        self._add_connection(self._connections[second], first)

    def find_room(self, rx: int, ry: int) -> Room | None:
        return next((rm for rm in self._rooms if rm.x == rx and rm.y == ry), None)

    def set_start_room(self, rx: int, ry: int):
        room = self.find_room(rx, ry)
        if room is not self._start_room:
            self._start_room = room
            self._cache_key += 1

    def set_finish_room(self, rx: int, ry: int):
        room = self.find_room(rx, ry)
        if room is not self._finish_room:
            self._finish_room = room
            self._cache_key += 1

    def set_dangerous_room(self, rx: int, ry: int):
        room = self.find_room(rx, ry)
        if room not in self._dangerous_rooms:
            self._dangerous_rooms.add(room)
            self._cache_key += 1
