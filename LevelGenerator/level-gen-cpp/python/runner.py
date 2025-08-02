from collections import defaultdict
import re
import subprocess
from time import time

num_models = 0
width = 10
height = 7
seed = 1234
min_rooms = 2
max_rooms = 6

args = (r"C:\Source\warp-space\LevelGenerator\clingo-exe\clingo.exe"
        f" {num_models} -c width={width} -c height={height}"
        f" -c min_rooms={min_rooms} -c max_rooms={max_rooms}"
        f" -t 4 --rand-freq=1.0 --seed={seed}"
        # ' -o reify'
        # ' --verbose'
        r" C:\Source\warp-space\LevelGenerator\level-gen-cpp\programs\ship.lp")

start = time()
ret = subprocess.run(args, text=True, stdout=subprocess.PIPE, stderr=subprocess.PIPE)
end = time()

print(f'Solving took {end - start}s')

data = ret.stdout
if ('SATISFIABLE' not in data and 'OPTIMUM FOUND' not in data) or 'UNSATISFIABLE' in data:
    raise ChildProcessError(f'failed to generate level:{f'\n{ret.stderr}' if ret.stderr else ''}{f'\n{data}' if data else ''}')


def yield_ints(pattern, inp):
    for mch in pattern.findall(inp):
        if isinstance(mch, tuple):
            yield tuple(map(int, mch))
        else:
            yield int(mch),


print([opt[0] for opt in yield_ints(re.compile(r'Optimization: (\d+)'), data)])

data = data[data.rfind(f'Answer: '):]
print(re.findall(r'initial_room\(\d+,\d+,\d+,\d+\)', data))

hull_square = re.compile(r'hull\((\d+),(\d+)\)')
in_space = re.compile(r'in_space\((\d+),(\d+)\)')
ship_square = re.compile(r'ship\((\d+),(\d+)\)')
room = re.compile(r'room\(\d+,\d+,\d+,\d+\)')
corridor_square = re.compile(r'corridor\((\d+),(\d+)\)')
room_square = re.compile(r'room_square\((\d+),(\d+),(\d+),(\d+),(\d+),(\d+)\)')
reachable = re.compile(r'reachable\(room\((\d+),(\d+),(\d+),(\d+)\)\)')
adjacent = re.compile(r'adjacent\(room\((\d+),(\d+),(\d+),(\d+)\),room\((\d+),(\d+),(\d+),(\d+)\)\)')

rid = 1
cid = 1
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
    else:
        return f'R{rooms[r]}'


for x, y in yield_ints(hull_square, data):
    assign(x, y, '  H  ')


for mch in yield_ints(reachable, data):
    reachable_r.add(mch)

for mch in room.findall(data):
    all_r.add(mch)


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

print(len(reachable_r), len(all_r))
