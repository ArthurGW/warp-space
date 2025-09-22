import os

import seaborn as sns
import matplotlib.pyplot as plt
import pandas as pd

this_dir = os.path.dirname(__file__)

out_file_path = f'survey_results.csv'
print(f'Reading from: {out_file_path}')
plt.rcParams['font.size'] = 12

df = pd.read_csv(os.path.join(this_dir, out_file_path))
df.drop(columns=['Timestamp'], inplace=True)

fig = plt.figure(figsize=[10, 10])
p = sns.boxplot(df, orient='h', whis=(0, 100), width=0.5)
p.set_xticks([1, 2, 3, 4, 5])
fig.tight_layout()
plt.show()
