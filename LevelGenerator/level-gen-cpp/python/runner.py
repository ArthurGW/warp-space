import re
import subprocess
from collections import defaultdict
from time import time

num_models = 20
width = 14
height = 12
seed = 455035
min_rooms = 3
max_rooms = 8
num_breaches = 1

args = (r"C:\Source\warp-space\LevelGenerator\clingo-exe\clingo.exe"
        f" {num_models} -c width={width} -c height={height} -c num_breaches={num_breaches}"
        f" -c min_rooms={min_rooms} -c max_rooms={max_rooms}"
        f" -t 4 --rand-freq=1.0 --seed={seed}"
        r" C:\Source\warp-space\LevelGenerator\level-gen-cpp\programs\ship.lp")

start = time()
ret = subprocess.run(args, text=True, stdout=subprocess.PIPE, stderr=subprocess.PIPE)
end = time()

print(f'Solving took {end - start}s')

data = ret.stdout
if ('SATISFIABLE' not in data and 'OPTIMUM FOUND' not in data) or 'UNSATISFIABLE' in data:
    raise ChildProcessError(
        f'failed to generate level:{f'\n{ret.stderr}' if ret.stderr else ''}{f'\n{data}' if data else ''}')

print(data)

rid = 1
bid = 1
cid = 1
breaches = {}
rooms = {}
corridors = {}
ship = [['     ' for __ in range(width)] for _ in range(height)]
reachable_r = set()
all_r = set()

adjacency = defaultdict(list)


def assign(x, y, v):
    ship[y - 1][x - 1] = v


def room_name(r):
    if r in corridors:
        return f'C{corridors[r]}'
    elif r in breaches:
        return f'B{breaches[r]}'
    else:
        return f'R{rooms[r]}'


def yield_ints(pattern, inp):
    for mch in pattern.findall(inp):
        if isinstance(mch, tuple):
            yield tuple(map(int, mch))
        else:
            yield int(mch),


print([opt[0] for opt in yield_ints(re.compile(r'Optimization: (-?\d+\s*)*'), data)])

data = data[data.rfind(f'Answer: '):]

hull_square = re.compile(r'hull\((\d+),(\d+)\)')
in_space = re.compile(r'in_space\((\d+),(\d+)\)')
ship_square = re.compile(r'ship\((\d+),(\d+)\)')
breach_square = re.compile(r'breach_square\((\d+),(\d+)\)')
breach = re.compile(r'alien_breach\((\d+,\d+,\d+,\d+),room\((\d+),(\d+),(\d+),(\d+)\)\)')
room = re.compile(r'room\(\d+,\d+,\d+,\d+\)')
start_room = re.compile(r'start_room\(room\((\d+),(\d+),(\d+),(\d+)\)\)')
finish_room = re.compile(r'finish_room\(room\((\d+),(\d+),(\d+),(\d+)\)\)')
corridor_square = re.compile(r'corridor\((\d+),(\d+)\)')
room_square = re.compile(r'room_square\((\d+),(\d+),(\d+),(\d+),(\d+),(\d+)\)')
reachable = re.compile(r'reachable\(room\((\d+),(\d+),(\d+),(\d+)\)\)')
adjacent = re.compile(r'adjacent\(room\((\d+),(\d+),(\d+),(\d+)\),room\((\d+),(\d+),(\d+),(\d+)\)\)')


for x, y in yield_ints(hull_square, data):
    assign(x, y, '  H  ')

for mch in yield_ints(reachable, data):
    reachable_r.add(mch)

for mch in room.findall(data):
    all_r.add(mch)

for mch in breach.findall(data):
    x, y, w, h = map(int, mch[0].split(','))
    rm = tuple(map(int, mch[1:]))
    breaches[(x, y, w, h)] = bid
    bid += 1

    adjacency[(x, y, w, h)].append(rm)
    adjacency[rm].append((x, y, w, h))

    for sx in range(x, x + w):
        for sy in range(y, y + h):
            assign(sx, sy, f' B{breaches[(x, y, w, h)] : <2} ')

for mch in yield_ints(adjacent, data):
    adjacency[mch[:4]].append(mch[4:])

for x, y, rx, ry, rw, rh in yield_ints(room_square, data):
    if (rw, rh) == (1, 1):
        if (rx, ry, rw, rh) not in corridors:
            corridors[(rx, ry, rw, rh)] = cid
            cid += 1

        assign(x, y, f' C{corridors[(rx, ry, rw, rh)]: <2} ')
    else:
        if (rx, ry, rw, rh) not in rooms:
            rooms[(rx, ry, rw, rh)] = rid
            rid += 1

        assign(x, y, f' R{rooms[(rx, ry, rw, rh)]: <2} ')

for r in list(adjacency):
    adj_rooms = adjacency.pop(r)
    adjacency[room_name(r)] = [room_name(rr) for rr in adj_rooms]

for row in ship:
    print('|'.join(row))
    print('-' * (5 * len(row) + len(row) - 1))

for a, r in adjacency.items():
    print(f'{a}: {r}')

for rm in yield_ints(start_room, data):  # Should be only one
    print(f'Start Room: {room_name(rm)}')

for rm in yield_ints(finish_room, data):  # Should be only one
    print(f'Finish Room: {room_name(rm)}')
