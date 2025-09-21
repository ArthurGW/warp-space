import itertools
import os
import re
import subprocess
from time import time

num_models = 50
width = 18
height = 14
seed = 9639
min_rooms = 3
max_rooms = 12
num_breaches = 3
num_portals = 3

for params in itertools.product(
    ("-t 4,compete",  # parallel mode
     "-t 4,split",
     ""),
    (14,),  # program number
    # (10, 9, 8),
    # (2, 3, 4, 5, 6,),
):
    for config in (
            'auto',
            'jumpy',
            'tweety',
            'trendy',
            'frumpy',
            'crafty',
            'handy',
            'many'
    ):
        this_dir = os.path.dirname(__file__)

        # Found by piclasp to be fast
        piclasp_args = "--backprop --learn-explicit --no-gamma --eq=0 --sat-prepro=0 --trans-ext=integ --del-cfl=F,55 --heuristic=Domain,94 --restarts=no --strengthen=recursive,all --del-glue=4,1 --del-grow=1.9111,94.6281 --del-init=30.3279,19,12774 --deletion=ipHeap,30,lbd --lookahead=no --init-moms --local-restarts --contraction=no --del-estimate=2 --del-max=1803231815 --del-on-restart=4 --init-watches=first --loops=shared --otfs=1 --partial-check=30 --reverse-arcs=2 --save-progress=115 --score-other=no --score-res=multiset --sign-def=pos --update-lbd=0"

        args = (
            f"{os.path.join(this_dir, '..', 'clingo', 'clingo.exe')}"
            f" {num_models} -c width={width} -c height={height} -c num_breaches={num_breaches}"
            f" -c min_rooms={min_rooms} -c max_rooms={max_rooms} -c num_portals={num_portals}"

            f" --configuration={config}"
            f' {piclasp_args}'
            f' {params[0]}'
            " --time-limit=240"  # seconds
            f" --rand-freq=1.0 --seed={seed}"
            f" {os.path.abspath(os.path.join(this_dir, 'asp', f'ship{params[1]}.lp'))}"
            f" {os.path.abspath(os.path.join(this_dir, 'asp', f'portal{params[1]}.lp'))}"
        )

        print(f"PARAMS: {params}")
        print(f"CONFIG: {config}")

        start = time()
        ret = subprocess.run(args, text=True, stdout=subprocess.PIPE, stderr=subprocess.PIPE)
        end = time()

        print(f'Solving took {end - start}s')

        print(ret.stderr)
        data = ret.stdout

        ans_re = re.compile(r"Answer: \d+ \(Time: \d+\.\d+s\)")
        opt_re = re.compile(r'Optimization: -?\d+')

        for a, o in zip(ans_re.findall(data), opt_re.findall(data)):
            print(a, o)
        print()
