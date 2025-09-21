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
    (14,),  # program number
    # (10, 9, 8),
    # (2, 3, 4, 5, 6,),
    ("-t 4,compete",  # parallel mode
     "-t 4,split",
     "-e bt --project=show,3"),  # backtracking + projection - can't be run in parallel
    ("--restart-on-model --opt-mode=enum,9 --opt-stop=2",  # enumeration params
     "--opt-mode=enum,9",
     ""),
    ("",
     " --sat-prepro=3"  # full SAT prepro
     "--project=show,3",
     "--opt-stop=3"),  # stop on cost < N
    ("--opt-strategy=bb,hier",  # model guided opt
     "--opt-strategy=bb,inc",
     "--opt-strategy=bb,dec",
     "--opt-strategy=usc,oll,7",  # core guided opt
     "--opt-strategy=usc,oll,7 --opt-usc-shrink=rgs",  # Enable core-shrinking in core-guided optimization
     "--opt-strategy=usc,oll,7 --opt-usc-shrink=bin"),  # Enable core-shrinking in core-guided optimization
    ("--opt-heuristic=sign,model",
     "--heuristic=Domain",
     "--heuristic=Domain --dom-mod=factor,opt",
     "--heuristic=Berkmin",
     "--heuristic=Vsids",
     "--heuristic=Vsids --vsids-acids",
     "--heuristic=Vsids --init-moms"),
    ("--save-progress=50",
     "--save-progress=75"),
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

        args = (
            f"{os.path.join(this_dir, '..', 'clingo', 'clingo.exe')}"
            f" {num_models} -c width={width} -c height={height} -c num_breaches={num_breaches}"
            f" -c min_rooms={min_rooms} -c max_rooms={max_rooms} -c num_portals={num_portals}"

            f" --configuration={config}"
            f' {params[1:]}'
            " --time-limit=240"  # seconds
            f" --rand-freq=1.0 --seed={seed}"
            f" {os.path.abspath(os.path.join(this_dir, 'asp', f'ship{params[0]}.lp'))}"
            f" {os.path.abspath(os.path.join(this_dir, 'asp', f'portal{params[0]}.lp'))}"
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
