import os
import re
import subprocess
from time import time

import numpy as np
import pandas as pd

from metrics.room_count import RoomCount
from metrics.average_room_size import AverageRoomSize
from metrics.density import Density
from metrics.exploration import Exploration
from metrics.level import Level, Room
from metrics.map_linearity import MapLinearity
from metrics.metric import Metric
from metrics.path_redundancy import PathRedundancy
from metrics.proximity_to_danger import ProximityToDanger

num_models = 10
width = 16
height = 16
min_rooms = 3
max_rooms = 12
num_breaches = 3
num_portals = 1

piclasp_args = ("--backprop --learn-explicit --no-gamma --eq=0 --sat-prepro=0 --trans-ext=integ --del-cfl=F,55"
                " --heuristic=Domain,94 --restarts=no --strengthen=recursive,all --del-glue=4,1"
                " --del-grow=1.9111,94.6281 --del-init=30.3279,19,12774 --deletion=ipHeap,30,lbd --lookahead=no"
                " --init-moms --local-restarts --contraction=no --del-estimate=2 --del-max=1803231815"
                " --del-on-restart=4 --init-watches=first --loops=shared --otfs=1 --partial-check=30 --reverse-arcs=2"
                " --save-progress=115 --score-other=no --score-res=multiset --sign-def=pos --update-lbd=0")

this_dir = os.path.dirname(__file__)

ship_square = re.compile(r'ship\((\d+),(\d+)\)')
breach = re.compile(r'alien_breach\((\d+),(\d+),(\d+),(\d+),(\d+),(\d+)\)')
room = re.compile(r'room\((\d+),(\d+),(\d+),(\d+)\)')
start_room = re.compile(r'start_room\((\d+),(\d+)\)')
finish_room = re.compile(r'finish_room\((\d+),(\d+)\)')
connected = re.compile(r'connected\((\d+),(\d+),(\d+),(\d+)\)')
portal = re.compile(r'portal\((\d+),(\d+),(\d+),(\d+)\)')


def yield_ints(pattern, inp):
    for mch in pattern.findall(inp):
        if isinstance(mch, tuple):
            yield tuple(map(int, mch))
        else:
            yield int(mch),


metrics: list[Metric] = [
    Density(),
    Exploration(),
    MapLinearity(),
    PathRedundancy(),
    ProximityToDanger(),
    AverageRoomSize(4 * 4),  # 4x4 rooms are the max allowed size
    RoomCount(),
]
seed_key = 'Seed'
time_key = 'Generation Time'
results = {seed_key: [], time_key: [], }
results.update({metric.title(): [] for metric in metrics})
assert len(metrics) == len(results) - 2  # No duplicate titles

out_file = re.compile(r'results_(\d+)\.csv')
existing_out_files = [out_file.match(f) for f in os.listdir(this_dir)]
existing_out_files = list(filter(None, existing_out_files))
max_ind = max(int(mch.group(1)) for mch in existing_out_files) if existing_out_files else 0
out_file_path = f'results_{max_ind + 1}.csv'
print(f'Writing to: {out_file_path}')

for i in range(1, 11):
    seed = np.random.randint(np.iinfo('int32').max - 1)
    args = (
        f"{os.path.join(this_dir, '..', 'clingo', 'clingo.exe')}"
        f" {num_models} -c width={width} -c height={height} -c num_breaches={num_breaches}"
        f" -c min_rooms={min_rooms} -c max_rooms={max_rooms} -c num_portals={num_portals}"
        f" -t 4,split --rand-freq=1.0 --seed={seed} --configuration=jumpy {piclasp_args}"
        f" {os.path.abspath(os.path.join(this_dir, '..', '..', 'level-gen-cpp', 'programs', 'ship.lp'))}"
        f" {os.path.abspath(os.path.join(this_dir, '..', '..', 'level-gen-cpp', 'programs', 'connections.lp'))}"
    )

    start = time()
    try:
        ret = subprocess.run(args, text=True, stdout=subprocess.PIPE, stderr=subprocess.PIPE, timeout=60)
    except subprocess.TimeoutExpired:
        print(f'Level {i} timed out, seed: {seed}')
        continue  # Some random seeds are slow to generate levels for, mostly by chance - ignore them
    generation_time = time() - start

    data = ret.stdout
    if ('SATISFIABLE' not in data and 'OPTIMUM FOUND' not in data) or 'UNSATISFIABLE' in data:
        print(f'Level {i} unsolvable took: {generation_time}s')
        continue

    print(f'Solving level {i} took: {generation_time}s')

    data = data[data.rfind(f'Answer: '):]  # Last found level

    # Allow duplicates as portals and doors can go between rooms
    level = Level(len(ship_square.findall(data)), allow_duplicate_connections=True)

    for x, y, w, h in yield_ints(room, data):
        level.add_room(Room(x, y, w, h))

    for pattern in (connected, portal):
        for x1, y1, x2, y2 in yield_ints(pattern, data):
            level.add_connection(level.find_room(x1, y1), level.find_room(x2, y2))

    start_rooms = list(yield_ints(start_room, data))
    if len(start_rooms) != 1:
        continue  # Something went wrong
    level.set_start_room(*start_rooms[0])

    finish_rooms = list(yield_ints(finish_room, data))
    if len(finish_rooms) != 1:
        continue  # Something went wrong
    level.set_finish_room(*finish_rooms[0])

    for _x, _y, _w, _h, breached_x, breached_y in yield_ints(breach, data):
        level.set_dangerous_room(breached_x, breached_y)

    for metric in metrics:
        results[metric.title()].append(metric.score(level))

    results[time_key].append(generation_time)
    results[seed_key].append(seed)

    if len(results[metrics[0].title()]) % 10 == 0:
        # Save intermediate results in case of errors
        pd.DataFrame(results).to_csv(os.path.join(this_dir, out_file_path), index=False)

pd.DataFrame(results).to_csv(os.path.join(this_dir, out_file_path), index=False)
