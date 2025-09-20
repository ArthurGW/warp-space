# A script to run levels with the reify -> meta -> solve workflow given in the PCG book
# This proved too slow and memory-hungry to be practical

import re, os
import subprocess
from collections import defaultdict
from time import time

num_models = 1
width = 10
height = 8
seed = 1234
min_rooms = 2
max_rooms = 6

this_dir = os.path.dirname(__file__)

start = time()

args = (
    f"{os.path.join(this_dir, '..', 'clingo', 'clingo.exe')}"
    f" -c width={width} -c height={height}"
    f" -c min_rooms={min_rooms} -c max_rooms={max_rooms}"
    " --pre --rewrite-minimize"
    f" {os.path.abspath(os.path.join(this_dir, '..', 'asp', f'ship_with_level_concepts.lp'))}"
)
ret = subprocess.run(args, text=True, stdout=subprocess.PIPE, stderr=subprocess.PIPE)

args1 = (
    f"{os.path.join(this_dir, '..', 'clingo', 'clingo.exe')}"
    " -o reify --reify-sccs"
)
ret1 = subprocess.run(args1, text=True, stdout=subprocess.PIPE, stderr=subprocess.PIPE, input=ret.stdout)

args2 = (
    f"{os.path.join(this_dir, '..', 'clingo', 'clingo.exe')} -o intermediate"
    " - meta.lp metaD.lp metaC.lp"
)
ret2 = subprocess.run(args2, text=True, stdout=subprocess.PIPE, stderr=subprocess.PIPE, input=ret1.stdout)

args3 = (
    f"{os.path.join(this_dir, '..', 'clingo', 'clingo.exe')} {num_models} --mode=clasp --rand-freq=1.0 --seed={seed}"
)
ret3 = subprocess.run(args3, text=True, stdout=subprocess.PIPE, stderr=subprocess.PIPE, input=ret2.stdout)

end = time()
print(f'Solving took {end - start}s')

data = ret3.stdout
if ('SATISFIABLE' not in data and 'OPTIMUM FOUND' not in data) or 'UNSATISFIABLE' in data or 'error' in data.lower():
    raise ChildProcessError(
        f'failed to generate level:{f'\n{ret.stderr}' if ret.stderr else ''}{f'\n{data}' if data else ''}')


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
