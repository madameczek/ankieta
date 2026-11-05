"""Generates docs/db-schema.svg and docs/db-schema.md from one table list.

Keep TABLES and NOTES in step with src/polls/db/migrations.sql, then run from anywhere:
    python docs/tools/db-schema.py
"""
import os
import sys
from html import escape

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))

# (key, column, type) – key: PK / FK / PK,FK / UQ / ''
TABLES = {
    'users': [('PK', 'id', 'int identity'), ('UQ', 'name', 'nvarchar(100)'), ('', 'created_at', 'datetime2')],
    'user_poll': [('PK,FK', 'id_poll', 'int'), ('PK,FK', 'id_user', 'int'), ('', 'is_owner', 'bit')],
    'polls': [
        ('PK', 'id', 'int identity'), ('', 'title', 'nvarchar(200)'), ('', 'description', 'nvarchar(2000) NULL'),
        ('', 'created_at', 'datetime2'), ('', 'modified_at', 'datetime2'), ('', 'deleted_at', 'datetime2 NULL'),
    ],
    'questions': [
        ('PK', 'id', 'int identity'), ('FK', 'id_poll', 'int'), ('', 'sort_order', 'int'),
        ('', 'question', 'nvarchar(1000)'), ('', 'modified_at', 'datetime2'), ('', 'deleted_at', 'datetime2 NULL'),
    ],
    'answers': [
        ('PK', 'id', 'int identity'), ('FK', 'id_question', 'int'), ('FK', 'id_user', 'int'),
        ('', 'answer', 'nvarchar(4000)'), ('', 'modified_at', 'datetime2'),
    ],
    'threshold_warnings': [('PK', 'resource', "varchar(20)"), ('', 'sent_at', 'datetime2')],
    'data_protection_keys': [('PK', 'id', 'int identity'), ('', 'friendly_name', 'nvarchar(max) NULL'), ('', 'xml', 'nvarchar(max) NULL')],
}

NOTES = [
    'user_poll: one owner per poll – unique (id_poll) WHERE is_owner = 1',
    'questions: unique (id_poll, sort_order) WHERE deleted_at IS NULL',
    'answers: unique (id_question, id_user) – one answer per user and question',
    'Soft delete: polls.deleted_at, questions.deleted_at (hidden by query filters)',
    'CHECK not empty: users.name, polls.title, questions.question, answers.answer',
    "threshold_warnings.resource IN ('users', 'polls'); dates are UTC (SYSUTCDATETIME())",
]

# ---------------------------------------------------------------- SVG
W = 300          # table width
HEAD = 30        # title bar
ROW = 22
PAD = 5

POS = {          # x, y
    'users': (30, 70), 'user_poll': (380, 70), 'polls': (730, 70),
    'threshold_warnings': (30, 330), 'answers': (380, 330), 'questions': (730, 330),
    'data_protection_keys': (30, 450),
}


def height(t):
    return HEAD + 2 * PAD + ROW * len(TABLES[t])


def row_y(t, i):
    return POS[t][1] + HEAD + PAD + ROW * i + ROW / 2


def table_svg(t):
    x, y = POS[t]
    h = height(t)
    out = [f'<g class="table">',
           f'<rect class="box" x="{x}" y="{y}" width="{W}" height="{h}" rx="8"/>',
           f'<path class="head" d="M{x} {y + HEAD} V{y + 8} Q{x} {y} {x + 8} {y} H{x + W - 8} Q{x + W} {y} {x + W} {y + 8} V{y + HEAD} Z"/>',
           f'<text class="title" x="{x + 12}" y="{y + 20}">{t}</text>']
    for i, (key, col, typ) in enumerate(TABLES[t]):
        cy = row_y(t, i) + 4.5
        if i % 2 == 1:
            out.append(f'<rect class="stripe" x="{x + 1}" y="{row_y(t, i) - ROW / 2}" width="{W - 2}" height="{ROW}"/>')
        if key:
            out.append(f'<text class="key" x="{x + 12}" y="{cy}">{key}</text>')
        out.append(f'<text class="col{" pk" if "PK" in key else ""}" x="{x + 62}" y="{cy}">{escape(col)}</text>')
        out.append(f'<text class="type" x="{x + W - 12}" y="{cy}" text-anchor="end">{escape(typ)}</text>')
    out.append('</g>')
    return '\n'.join(out)


def rel(path, one, many):
    """path + '1' / '*' labels at given points."""
    return (f'<path class="rel" d="{path}"/>'
            f'<text class="card" x="{one[0]}" y="{one[1]}">1</text>'
            f'<text class="card" x="{many[0]}" y="{many[1]}">*</text>')


ux, _ = POS['users']; upx, _ = POS['user_poll']; px, _ = POS['polls']; ax, _ = POS['answers']; qx, _ = POS['questions']
u_id = row_y('users', 0)
rels = [
    # users.id → user_poll.id_user and answers.id_user (shared trunk)
    rel(f'M{ux + W} {u_id} H350 V{row_y("user_poll", 1)} H{upx}', (ux + W + 5, u_id - 6), (upx - 13, row_y('user_poll', 1) - 6)),
    rel(f'M350 {u_id} V{row_y("answers", 2)} H{ax}', (ux + W + 5, u_id - 6), (ax - 13, row_y('answers', 2) - 6)),
    # polls.id → user_poll.id_poll
    rel(f'M{px} {row_y("polls", 0)} H{upx + W}', (px - 13, row_y('polls', 0) - 6), (upx + W + 5, row_y('user_poll', 0) - 6)),
    # polls.id → questions.id_poll (right side)
    rel(f'M{px + W} {row_y("polls", 0)} H1055 V{row_y("questions", 1)} H{qx + W}', (px + W + 5, row_y('polls', 0) - 6), (qx + W + 5, row_y('questions', 1) - 6)),
    # questions.id → answers.id_question
    rel(f'M{qx} {row_y("questions", 0)} H705 V{row_y("answers", 1)} H{ax + W}', (qx - 13, row_y('questions', 0) - 6), (ax + W + 5, row_y('answers', 1) - 6)),
]

notes_y = 515
svg = f'''<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 1080 700" width="1080" height="700" role="img" aria-labelledby="t d">
<title id="t">polls – database schema</title>
<desc id="d">Tables users, user_poll, polls, questions, answers with their relations, plus threshold_warnings and data_protection_keys.</desc>
<style>
  svg {{ --bg: #f9f9ff; --box: #ffffff; --head: #d8e2ff; --head-text: #001a41; --line: #74777f; --text: #1a1b20;
         --muted: #5c5f67; --key: #8a5a00; --stripe: #f1f3fa; --rel: #3d6bc4; }}
  @media (prefers-color-scheme: dark) {{
    svg {{ --bg: #111318; --box: #1d2024; --head: #2a3a5c; --head-text: #d8e2ff; --line: #8e9099; --text: #e2e2e9;
           --muted: #a9abb4; --key: #ffb84d; --stripe: #24272c; --rel: #8fb1ff; }}
  }}
  .bg {{ fill: var(--bg); }}
  .box {{ fill: var(--box); stroke: var(--line); stroke-width: 1; }}
  .head {{ fill: var(--head); }}
  .stripe {{ fill: var(--stripe); }}
  text {{ font-family: "Segoe UI", Roboto, Helvetica, Arial, sans-serif; fill: var(--text); font-size: 13px; }}
  .title {{ font-size: 15px; font-weight: 600; fill: var(--head-text); }}
  .key {{ font-size: 11px; font-weight: 700; fill: var(--key); }}
  .pk {{ font-weight: 600; }}
  .type {{ font-family: Consolas, "Cascadia Mono", Menlo, monospace; font-size: 12px; fill: var(--muted); }}
  .rel {{ fill: none; stroke: var(--rel); stroke-width: 1.6; }}
  .card {{ font-size: 13px; font-weight: 700; fill: var(--rel); }}
  .h1 {{ font-size: 20px; font-weight: 600; }}
  .caption {{ font-size: 12px; fill: var(--muted); }}
  .note {{ font-size: 12px; fill: var(--muted); }}
</style>
<rect class="bg" width="1080" height="700"/>
<text class="h1" x="30" y="40">polls – database schema</text>
<text class="caption" x="1050" y="40" text-anchor="end">source: src/polls/db/migrations.sql</text>
{chr(10).join(rels)}
{chr(10).join(table_svg(t) for t in TABLES)}
<text class="caption" x="30" y="318">no relations (infrastructure)</text>
<text class="h1" x="380" y="{notes_y}" style="font-size:14px">Constraints</text>
{chr(10).join(f'<text class="note" x="380" y="{notes_y + 22 + 20 * i}">• {escape(n)}</text>' for i, n in enumerate(NOTES))}
<text class="note" x="30" y="{notes_y + 150}">PK primary key · FK foreign key · UQ unique · 1 ── * one-to-many · NULL = optional, all other columns NOT NULL</text>
</svg>
'''
open(os.path.join(ROOT, 'docs', 'db-schema.svg'), 'w', encoding='utf-8', newline='\n').write(svg)

# ---------------------------------------------------------------- semigraphic
def box_lines(t, total=0):
    rows = TABLES[t]
    kw = max(len(k) for k, _, _ in rows)
    nw = max(len(c) for _, c, _ in rows)
    body = [f' {k:<{kw}}  {c:<{nw}}  {ty} ' for k, c, ty in rows]
    inner = max(len(t) + 2, total - 2, *(len(b) for b in body))
    lines = ['┌' + '─' * inner + '┐', '│' + f' {t}'.ljust(inner) + '│', '├' + '─' * inner + '┤']
    lines += ['│' + b.ljust(inner) + '│' for b in body]
    lines.append('└' + '─' * inner + '┘')
    return lines


GAP = 9
cols = [['users', 'threshold_warnings', 'data_protection_keys'], ['user_poll', 'answers'], ['polls', 'questions']]
natural = {t: len(box_lines(t)[0]) for t in TABLES}
colw = [max(natural[t] for t in c) for c in cols]
width = {t: colw[i] for i, c in enumerate(cols) for t in c}
colx = [0, colw[0] + GAP, colw[0] + GAP + colw[1] + GAP]
top = {'users': 0, 'user_poll': 0, 'polls': 0, 'threshold_warnings': 13, 'answers': 13, 'questions': 13, 'data_protection_keys': 19}
X = {t: colx[i] for i, c in enumerate(cols) for t in c}
H = max(top[t] + len(box_lines(t)) for t in TABLES)
Wc = colx[2] + colw[2] + 4
grid = [[' '] * Wc for _ in range(H)]

for t in TABLES:
    for r, line in enumerate(box_lines(t, width[t])):
        for c, ch in enumerate(line):
            grid[top[t] + r][X[t] + c] = ch


def rowr(t, i):  # grid row of column i
    return top[t] + 3 + i


def put(r, c, ch):
    grid[r][c] = ch


def hline(r, c1, c2):
    for c in range(min(c1, c2), max(c1, c2) + 1):
        put(r, c, '─')


def vline(c, r1, r2):
    for r in range(min(r1, r2), max(r1, r2) + 1):
        put(r, c, '│')


R = lambda t: X[t] + width[t] - 1   # right border column
L = lambda t: X[t]                  # left border column

# users.id → user_poll.id_user, answers.id_user (trunk at column tx)
tx = R('users') + 4
r0 = rowr('users', 0)
hline(r0, R('users') + 1, tx); put(r0, R('users') + 1, '1'); put(r0, tx, '┐')
r1 = rowr('user_poll', 1); r2 = rowr('answers', 2)
vline(tx, r0 + 1, r2 - 1)
put(r1, tx, '├'); hline(r1, tx + 1, L('user_poll') - 1); put(r1, L('user_poll') - 1, '*')
put(r2, tx, '└'); hline(r2, tx + 1, L('answers') - 1); put(r2, L('answers') - 1, '*')

# polls.id → user_poll.id_poll
r = rowr('polls', 0)
hline(r, R('user_poll') + 1, L('polls') - 1); put(r, R('user_poll') + 1, '*'); put(r, L('polls') - 1, '1')

# polls.id → questions.id_poll (right side)
rx = R('polls') + 3
rq = rowr('questions', 1)
hline(r, R('polls') + 1, rx); put(r, R('polls') + 1, '1'); put(r, rx, '┐')
vline(rx, r + 1, rq - 1); put(rq, rx, '┘'); hline(rq, R('questions') + 1, rx - 1); put(rq, R('questions') + 1, '*')

# questions.id → answers.id_question
ra = rowr('questions', 0); rb = rowr('answers', 1)
mx = R('answers') + 4
hline(ra, mx + 1, L('questions') - 1); put(ra, L('questions') - 1, '1'); put(ra, mx, '┌')
put(rb, mx, '┘'); hline(rb, R('answers') + 1, mx - 1); put(rb, R('answers') + 1, '*')

diagram = '\n'.join(''.join(row).rstrip() for row in grid)

md = f'''# Database schema

Diagram: [db-schema.svg](db-schema.svg). Source of truth: `src/polls/db/migrations.sql` (decisions D3–D9, D31).

```text
{diagram}
```

`1 ─ *` one-to-many · PK primary key · FK foreign key · UQ unique · `NULL` = optional, all other columns NOT NULL.
`threshold_warnings` and `data_protection_keys` have no relations (thresholds D14, Data Protection keys D22).

Constraints:
{chr(10).join('- ' + n for n in NOTES)}
'''
open(os.path.join(ROOT, 'docs', 'db-schema.md'), 'w', encoding='utf-8', newline='\n').write(md)
sys.stdout.reconfigure(encoding="utf-8")
print(diagram)
