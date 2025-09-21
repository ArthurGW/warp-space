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
min_rooms = 3
max_rooms = 12

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

num_warps_key = 'Num Warps'
seed_key = 'Seed'
time_key = 'Generation Time'
width_key = 'Width'
height_key = 'Height'
breaches_key = 'Num Breaches'
portals_key = 'Num Portals'

results = {key: [] for key in (num_warps_key, seed_key, time_key, width_key, height_key, breaches_key, portals_key)}
results.update({metric.title(): [] for metric in metrics})
assert len(metrics) + len((num_warps_key, seed_key, time_key, width_key, height_key, breaches_key, portals_key)) == len(results)  # No duplicate titles

out_file = re.compile(r'results_per_level_(\d+)\.csv')
existing_out_files = [out_file.match(f) for f in os.listdir(this_dir)]
existing_out_files = list(filter(None, existing_out_files))
max_ind = max(int(mch.group(1)) for mch in existing_out_files) if existing_out_files else 0
out_file_path = f'results_per_level_{max_ind + 1}.csv'
print(f'Writing to: {out_file_path}')

level_params = [
    {
        num_warps_key: i,
        breaches_key: int(1 + np.floor(i * 0.5)),
        portals_key: int(np.floor(i * 0.5)),
    }
    for i in range(0, 11, 2)
]

for param_set in level_params:
    for i in range(1, 201):
        seed = np.random.randint(np.iinfo('int32').max - 1)
        width = np.random.randint(16, 19)
        height = np.random.randint(7, 9) * 2

        args = (
            f"{os.path.join(this_dir, '..', 'clingo', 'clingo.exe')}"
            f" {num_models} -c width={width} -c height={height}"
            f" -c num_breaches={param_set[breaches_key]} -c num_portals={param_set[portals_key]}"
            f" -c min_rooms={min_rooms} -c max_rooms={max_rooms}"
            f" -t 4,split --rand-freq=1.0 --seed={seed} --configuration=jumpy {piclasp_args}"
            f" {os.path.abspath(os.path.join(this_dir, '..', '..', 'level-gen-cpp', 'programs', 'ship.lp'))}"
            f" {os.path.abspath(os.path.join(this_dir, '..', '..', 'level-gen-cpp', 'programs', 'connections.lp'))}"
        )

        start = time()
        try:
            ret = subprocess.run(args, text=True, stdout=subprocess.PIPE, stderr=subprocess.PIPE, timeout=60)
        except subprocess.TimeoutExpired:
            print(f'Level {param_set[num_warps_key]}:{i} timed out, w,h: {(width, height)}')
            continue  # Some random seeds are slow to generate levels for, mostly by chance - ignore them
        generation_time = time() - start

        data = ret.stdout
        if ('SATISFIABLE' not in data and 'OPTIMUM FOUND' not in data) or 'UNSATISFIABLE' in data:
            print(f'Level {param_set[num_warps_key]}:{i} unsolvable took: {generation_time}s')
            continue

        print(f'Solving level {param_set[num_warps_key]}:{i} took: {generation_time}s')

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

        for key in (num_warps_key, breaches_key, portals_key):
            results[key].append(param_set[key])
        results[width_key].append(width)
        results[height_key].append(height)
        results[seed_key].append(seed)
        results[time_key].append(generation_time)

        if len(results[num_warps_key]) % 50 == 0:
            # Save intermediate results in case of errors
            pd.DataFrame(results).to_csv(os.path.join(this_dir, out_file_path), index=False)

pd.DataFrame(results).to_csv(os.path.join(this_dir, out_file_path), index=False)
