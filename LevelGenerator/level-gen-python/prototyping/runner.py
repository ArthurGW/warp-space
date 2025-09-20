import os
import re
import subprocess
from collections import defaultdict
from time import time

num_models = 1
width = 9
height = 10
seed = 1234
min_rooms = 1
max_rooms = 6
num_breaches = 1
num_portals = 0

piclasp_args = news = "--backprop --learn-explicit --no-gamma --eq=0 --sat-prepro=0 --trans-ext=integ --del-cfl=F,55 --heuristic=Domain,94 --restarts=no --strengthen=recursive,all --del-glue=4,1 --del-grow=1.9111,94.6281 --del-init=30.3279,19,12774 --deletion=ipHeap,30,lbd --lookahead=no --init-moms --local-restarts --contraction=no --del-estimate=2 --del-max=1803231815 --del-on-restart=4 --init-watches=first --loops=shared --otfs=1 --partial-check=30 --reverse-arcs=2 --save-progress=115 --score-other=no --score-res=multiset --sign-def=pos --update-lbd=0"

this_dir = os.path.dirname(__file__)

args = (
    f"{os.path.join(this_dir, 'clingo.exe')}"
    f" {num_models} -c width={width} -c height={height} -c num_breaches={num_breaches}"
    f" -c min_rooms={min_rooms} -c max_rooms={max_rooms} -c num_portals={num_portals}"
    f" -t 1 --rand-freq=1.0 --seed={seed} --configuration=jumpy {piclasp_args}"
    f" {os.path.abspath(os.path.join(this_dir, '..', 'asp', 'ship.lp'))}"
    f" {os.path.abspath(os.path.join(this_dir, '..', 'asp', 'portal.lp'))}"
)

start = time()
ret = subprocess.run(args, text=True, stdout=subprocess.PIPE, stderr=subprocess.PIPE)
end = time()

print(f'Solving took {end - start}s')

data = ret.stdout
if ('SATISFIABLE' not in data and 'OPTIMUM FOUND' not in data) or 'UNSATISFIABLE' in data:
    raise ChildProcessError(
        f'failed to generate level:{f'\n{ret.stderr}' if ret.stderr else ''}{f'\n{data}' if data else ''}')

print(ret.stderr)
print(data)

rid = 1
bid = 1
cid = 1
breaches = {}
rooms = {}
corridors = {}
ship = [['     ' for __ in range(width)] for _ in range(height)]
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
corridor_square = re.compile(r'corridor\((\d+),(\d+)\)')
breach_square = re.compile(r'breach_square\((\d+),(\d+),(\d+),(\d+),(\d+),(\d+)\)')
room_square = re.compile(r'room_square\((\d+),(\d+),(\d+),(\d+),(\d+),(\d+)\)')
breach = re.compile(r'alien_breach\((\d+),(\d+),(\d+),(\d+),(\d+),(\d+)\)')
room = re.compile(r'room\((\d+),(\d+),(\d+),(\d+)\)')
start_room = re.compile(r'start_room\((\d+),(\d+)\)')
finish_room = re.compile(r'finish_room\((\d+),(\d+)\)')
connected = re.compile(r'connected\((\d+),(\d+),(\d+),(\d+)\)')
portal = re.compile(r'portal\((\d+),(\d+),(\d+),(\d+)\)')

for x, y in yield_ints(hull_square, data):
    assign(x, y, '  H  ')

for mch in yield_ints(room, data):
    if mch[2] == 1:  # Corridor
        continue

    rooms[mch[:2]] = rid
    rid += 1
    all_r.add(mch[:2])

for mch in yield_ints(breach, data):
    xy = mch[:2]
    rm = mch[4:]
    breaches[xy] = bid
    bid += 1

    adjacency[xy].append((rm, False))
    adjacency[rm].append((xy, False))

for x, y, bx, by, bw, bh in yield_ints(breach_square, data):
    assign(x, y, f' B{breaches[(bx, by)]: <2} ')

for x1, y1, x2, y2 in yield_ints(connected, data):
    adjacency[(x1, y1)].append(((x2, y2), False))
    adjacency[(x2, y2)].append(((x1, y1), False))

for x1, y1, x2, y2 in yield_ints(portal, data):
    adjacency[(x1, y1)].append(((x2, y2), True))
    adjacency[(x2, y2)].append(((x1, y1), True))

for x, y, rx, ry, rw, rh in yield_ints(room_square, data):
    if (rx, ry) in rooms:
        assign(x, y, f' R{rooms[(rx, ry)]: <2} ')

for xy in yield_ints(corridor_square, data):
    if xy not in corridors:
        corridors[xy] = cid
        cid += 1

    assign(xy[0], xy[1], f' C{corridors[xy]: <2} ')

for row in ship:
    print('|'.join(row))
    print('-' * (5 * len(row) + len(row) - 1))

for r, adj in adjacency.items():
    print(f'{room_name(r)}: {[f'{room_name(rr)}{" (PORTAL)" if is_portal else ""}' for rr, is_portal in adj]}')

for rm in yield_ints(start_room, data):  # Should be only one
    print(f'Start Room: {room_name(rm)}')

for rm in yield_ints(finish_room, data):  # Should be only one
    print(f'Finish Room: {room_name(rm)}')
