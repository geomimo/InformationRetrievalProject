import pandas as pd
import numpy as np
import matplotlib.pyplot as plt


def interpolate(rs, ps):
    inter_ps = []
    ls = list(zip(rs, ps))
    sp = np.linspace(0,1,11)
    for x in sp:
        try:
            max_p = max(ls, key=lambda i: i[1])[1]
            inter_ps.append(max_p)
            ls = [i for i in ls if i[0]>x]
        except ValueError:
            break
    inter_ps.extend([inter_ps[-1]]*(11-len(inter_ps)))
    return inter_ps


def save(name):
    df = pd.read_csv('./history/' + name + '.csv')
    rs = df.loc[:,[f"R{i}" for i in range(8)]]
    ps = df.loc[:,[f"P{i}" for i in range(8)]]
    inter_ps = list(map(interpolate, rs.values, ps.values))
    inter_rs = np.linspace(0, 1, 11)
    for i in range(len(inter_ps)):
        fig = plt.figure()
        plt.plot(inter_rs, inter_ps[i], marker='o')
        plt.title(f'ID: {i+1}')
        plt.xlim((0,1.1))
        plt.ylim((0,1.1))
        plt.savefig(f'./plots/{name}/{i+1}.png')
        plt.close(fig)


save('default')
save('BM25')


