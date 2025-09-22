import os
import re
import sys

import seaborn as sns
import matplotlib.pyplot as plt
import pandas as pd

this_dir = os.path.dirname(__file__)

out_file = re.compile(r'results_per_level_(\d+)\.csv')
out_files = [out_file.match(f) for f in os.listdir(this_dir)]
out_files = list(filter(None, out_files))
if not out_files:
    sys.exit(1)  # Nothing to plot

max_ind = max(int(mch.group(1)) for mch in out_files)
out_file_path = f'results_per_level_{max_ind}.csv'
print(f'Reading from: {out_file_path}')
plt.rcParams['font.size'] = 18

df = pd.read_csv(os.path.join(this_dir, out_file_path))
df.rename(columns={
    'Map Linearity': 'Linearity',
    'Path Redundancy': 'Redundancy',
    'Proximity to Danger': 'Danger',
    'Average Room Size': 'Room Size'
}, inplace=True)
hue = df['Num Warps']
unique_hue = hue.unique()
pg = sns.PairGrid(df, corner=True, vars=['Exploration', 'Linearity', 'Redundancy', 'Danger', 'Room Size'])
pg.set(xlim=(0, 1), ylim=(0, 1))
palette = sns.color_palette('flare_r', as_cmap=True)

pg.map_diag(sns.kdeplot, hue=hue, palette=palette)
pg.map_lower(sns.histplot, stat='percent', cmap='viridis', bins=50, binrange=(0, 1), thresh=None, hue_norm=plt.Normalize(vmin=0, vmax=50))

cax = plt.axes((0.8, 0.3, 0.04, 0.4))
plt.colorbar(plt.cm.ScalarMappable(cmap='viridis', norm=plt.Normalize(0, 50)), cax=cax, label="Percentage of Levels", location='left')

colour_value_lookup = {tuple(palette(v/unique_hue.max())): v for v in unique_hue}
legend_values = {colour_value_lookup[line.get_color()]: line for line in pg.diag_axes[0].lines}
legend_data = {str(value): legend_values[value] for value in sorted(legend_values)}
pg.add_legend(legend_data=legend_data, label_order=list(legend_data), title='Num Warps', fontsize='large')

plt.show()
