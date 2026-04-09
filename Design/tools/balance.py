#!/usr/bin/env python3
"""
Game Balance Analysis Tool
CSV 데이터를 파싱하여 레시피 체인, ROI, 경제 밸런스를 분석합니다.

Usage:
    python balance.py                  # 기본 리포트 (score=1.0)
    python balance.py --score 0.5      # 특정 스코어로 분석
    python balance.py --chain R009     # 특정 레시피 체인 상세 출력
    python balance.py --compare        # 구체제(1.20 균일) vs 신체제 비교
"""

import os
import csv
import argparse
from collections import defaultdict

# ---------------------------------------------------------------------------
# Paths
# ---------------------------------------------------------------------------
SCRIPT_DIR = os.path.dirname(os.path.abspath(__file__))
DATA_DIR = os.path.normpath(
    os.path.join(SCRIPT_DIR, '..', '..', 'Assets', 'Resources', 'driveAssets', 'dataTables')
)

# ---------------------------------------------------------------------------
# Constants
# ---------------------------------------------------------------------------
MINIGAME_NAMES = {
    'M001': 'Bake',  'M002': 'Boil',  'M003': 'Steam',
    'M004': 'Mix',   'M005': 'Sauce', 'M006': 'Slice', 'M007': 'Grill',
}

WEIGHT_CATEGORY = {
    'Grains': '메인', 'Meat': '메인', 'Seafood': '메인',
    'Vegetables': '일반', 'Dairy': '일반',
    'Sauces': '소스', 'Liquid': '소스', 'Other': '소스',
}

EXPECTED_WEIGHTS = {'메인': 1.5, '일반': 1.3, '소스': 1.0}
PROCESSING_WEIGHT = 0.2

# Gameplay constants (from TimeManager / StatsSystem)
SESSION_SECONDS = 80        # 영업시간 ~80초 (게임 내 4시간, 180배속)
MINIGAME_SECONDS = 10       # 미니게임 1회 평균 소요
STARTING_MONEY = 3000

# Customer reward multipliers (from MenuValidator.CalculateReward)
REWARD_ALL_MATCH = 1.3
REWARD_MAIN_ONLY = 1.0
REWARD_NO_MATCH = 0.7
REWARD_PER_SIDE = 0.1       # +10% per matching side


# ---------------------------------------------------------------------------
# Data Loading
# ---------------------------------------------------------------------------
def load_csv(filename):
    filepath = os.path.join(DATA_DIR, filename)
    with open(filepath, 'r', encoding='utf-8') as f:
        return list(csv.DictReader(f))


def load_ingredients():
    result = {}
    for row in load_csv('ingredient.csv'):
        result[row['id']] = {
            'display': row['display'],
            'price': int(row['defaultPrice']),
            'tags': row['tags'],
        }
    return result


def load_recipes():
    result = {}
    for row in load_csv('recipe.csv'):
        inputs = []
        if row['InputInfoList']:
            for item in row['InputInfoList'].split('/'):
                food_id, weight = item.split('-')
                inputs.append({'food_id': food_id, 'weight': float(weight)})
        result[row['id']] = {
            'output_id': row['OutputId'],
            'minigame_id': row['MinigameId'],
            'inputs': inputs,
        }
    return result


def load_foods():
    result = {}
    for row in load_csv('food.csv'):
        result[row['id']] = {
            'name': row['name'],
            'type': row['type'],
            'tools': row['availableTool'].split('/') if row['availableTool'] else [],
        }
    return result


# ---------------------------------------------------------------------------
# Analysis Engine
# ---------------------------------------------------------------------------
class BalanceAnalyzer:
    def __init__(self, ingredients, recipes, foods):
        self.ingredients = ingredients
        self.recipes = recipes
        self.foods = foods
        self.output_to_recipe = {
            r['output_id']: rid
            for rid, r in recipes.items()
            if r['output_id']
        }

    def food_name(self, food_id):
        return self.foods.get(food_id, {}).get('name', food_id)

    def food_type(self, food_id):
        return self.foods.get(food_id, {}).get('type', '?')

    # ---- Core price calculation (matches CookingToolSchema.Cook) ----
    def calc_recipe(self, recipe_id, score=1.0):
        """Recursively calculate recipe output.
        Returns (output_price, raw_cost, minigame_count, chain_steps).
        Price truncation matches C# (int) cast at each Cook() call.
        """
        recipe = self.recipes[recipe_id]
        total = 0.0
        raw_cost = 0
        games = 1 if recipe['minigame_id'] else 0
        steps = []

        for inp in recipe['inputs']:
            fid, w = inp['food_id'], inp['weight']

            if fid in self.output_to_recipe:
                sub_price, sub_raw, sub_games, sub_steps = self.calc_recipe(
                    self.output_to_recipe[fid], score
                )
                total += sub_price * (1 + w * score)
                raw_cost += sub_raw
                games += sub_games
                steps.extend(sub_steps)
            else:
                base = self.ingredients.get(fid, {}).get('price', 0)
                total += base * (1 + w * score)
                raw_cost += base

        output_price = int(total)  # C# (int) truncation

        steps.append({
            'recipe_id': recipe_id,
            'output_id': recipe['output_id'],
            'name': self.food_name(recipe['output_id']),
            'type': self.food_type(recipe['output_id']),
            'minigame': recipe['minigame_id'],
            'price': output_price,
            'raw_inputs': [
                (inp['food_id'], self.food_name(inp['food_id']), inp['weight'])
                for inp in recipe['inputs']
            ],
        })
        return output_price, raw_cost, games, steps

    # ---- Customer reward calculation (matches MenuValidator) ----
    @staticmethod
    def calc_reward(main_price, side_prices, all_match=True):
        """Calculate customer reward with matching bonuses."""
        n_sides = len(side_prices)
        base = main_price + sum(side_prices)
        side_bonus = 1.0 + n_sides * REWARD_PER_SIDE
        final_mult = REWARD_ALL_MATCH if all_match else REWARD_MAIN_ONLY
        return int(base * side_bonus * final_mult)

    # ---- Aggregated analyses ----
    def get_final_dishes(self):
        results = []
        for rid, recipe in self.recipes.items():
            ft = self.food_type(recipe['output_id'])
            if ft not in ('MAIN', 'SIDE'):
                continue

            p1, raw, games, steps = self.calc_recipe(rid, 1.0)
            p05, _, _, _ = self.calc_recipe(rid, 0.5)
            p0, _, _, _ = self.calc_recipe(rid, 0.0)
            roi = (p1 - raw) / raw * 100 if raw else 0
            rpg = roi / games if games else 0
            profit = p1 - raw

            results.append({
                'recipe_id': rid, 'name': self.food_name(recipe['output_id']),
                'type': ft, 'games': games, 'raw_cost': raw,
                'sell_1': p1, 'sell_05': p05, 'sell_0': p0,
                'profit': profit, 'roi': roi, 'rpg': rpg,
                'steps': steps,
            })
        return sorted(results, key=lambda d: -d['rpg'])

    def get_ingredient_usage(self):
        usage = defaultdict(list)
        for rid, recipe in self.recipes.items():
            for inp in recipe['inputs']:
                usage[inp['food_id']].append(rid)
        return usage

    def get_minigame_dist(self):
        dist = defaultdict(list)
        for rid, recipe in self.recipes.items():
            if recipe['minigame_id']:
                dist[recipe['minigame_id']].append(rid)
        return dist

    def find_dead_data(self):
        usage = self.get_ingredient_usage()
        warns = []
        for iid in self.ingredients:
            if iid not in self.foods:
                warns.append(f'{iid}: ingredient.csv에만 존재, food.csv에 없음')
            if iid not in usage:
                warns.append(f'{iid} ({self.food_name(iid)}): 레시피 미사용')
        return warns

    def check_weight_consistency(self):
        issues = []
        for rid, recipe in self.recipes.items():
            for inp in recipe['inputs']:
                fid, w = inp['food_id'], inp['weight']
                ft = self.food_type(fid)
                if ft == 'PROCESSING':
                    if abs(w - PROCESSING_WEIGHT) > 0.01:
                        issues.append(
                            f'{rid}: {fid}({self.food_name(fid)}) PROCESSING '
                            f'weight={w:.2f} (expected {PROCESSING_WEIGHT})'
                        )
                elif fid in self.ingredients:
                    tag = self.ingredients[fid].get('tags', '')
                    cat = WEIGHT_CATEGORY.get(tag, '')
                    expected = EXPECTED_WEIGHTS.get(cat)
                    if expected and abs(w - expected) > 0.01:
                        issues.append(
                            f'{rid}: {fid}({self.food_name(fid)}) '
                            f'{tag}→{cat} weight={w:.2f} (expected {expected})'
                        )
        return issues

    def best_daily_combos(self, top_n=3):
        """Find best MAIN+SIDE combos by daily profit."""
        dishes = self.get_final_dishes()
        mains = [d for d in dishes if d['type'] == 'MAIN']
        sides = [d for d in dishes if d['type'] == 'SIDE']
        combos = []
        for m in mains:
            for s in sides:
                total_games = m['games'] + s['games']
                orders = int(SESSION_SECONDS / MINIGAME_SECONDS / total_games)
                orders = max(orders, 1)
                reward = self.calc_reward(m['sell_1'], [s['sell_1']])
                cost = m['raw_cost'] + s['raw_cost']
                daily_profit = (reward - cost) * orders
                combos.append({
                    'main': m['name'], 'side': s['name'],
                    'games': total_games, 'orders': orders,
                    'reward': reward, 'cost': cost,
                    'daily_profit': daily_profit,
                })
        combos.sort(key=lambda c: -c['daily_profit'])
        return combos[:top_n]

    def day1_options(self):
        """What can you cook with starting money?"""
        options = []
        for rid, recipe in self.recipes.items():
            if not recipe['inputs']:
                continue
            _, raw, games, _ = self.calc_recipe(rid, 1.0)
            if raw <= STARTING_MONEY:
                sell, _, _, _ = self.calc_recipe(rid, 1.0)
                options.append({
                    'recipe_id': rid,
                    'name': self.food_name(recipe['output_id']),
                    'type': self.food_type(recipe['output_id']),
                    'cost': raw, 'sell': sell, 'games': games,
                })
        options.sort(key=lambda o: -(o['sell'] - o['cost']))
        return options


# ---------------------------------------------------------------------------
# Report Printer
# ---------------------------------------------------------------------------
W = 72  # report width


def hr(char='─'):
    print(char * W)


def section(title):
    print()
    print(f'  {title}')
    hr()


def print_report(analyzer):
    dishes = analyzer.get_final_dishes()
    usage = analyzer.get_ingredient_usage()
    mg_dist = analyzer.get_minigame_dist()
    dead = analyzer.find_dead_data()
    weight_issues = analyzer.check_weight_consistency()
    combos = analyzer.best_daily_combos()
    day1 = analyzer.day1_options()

    hr('═')
    print('  GAME BALANCE REPORT')
    hr('═')

    # ── Final Dishes ──
    for dish_type in ('MAIN', 'SIDE'):
        td = [d for d in dishes if d['type'] == dish_type]
        section(f'{dish_type} Dishes — Ranked by ROI/Game')
        print(f' {"#":<2} {"Name":<14} {"G":>2} {"Cost":>7} '
              f'{"@1.0":>9} {"@0.5":>9} {"ROI":>6} {"R/G":>5}')
        hr('·')
        for i, d in enumerate(td, 1):
            print(f' {i:<2} {d["name"]:<14} {d["games"]:>2} '
                  f'{d["raw_cost"]:>6,}G '
                  f'{d["sell_1"]:>8,}G {d["sell_05"]:>8,}G '
                  f'{d["roi"]:>5.0f}% {d["rpg"]:>4.0f}%')

    # ── Best Daily Combos ──
    section(f'Best Daily Combos (영업 {SESSION_SECONDS}초, 미니게임 {MINIGAME_SECONDS}초/회)')
    print(f' {"#":<2} {"Main":<12} {"Side":<12} '
          f'{"G":>2} {"주문":>3} {"보상":>9} {"일일이익":>10}')
    hr('·')
    for i, c in enumerate(combos, 1):
        print(f' {i:<2} {c["main"]:<12} {c["side"]:<12} '
              f'{c["games"]:>2} {c["orders"]:>3}회 '
              f'{c["reward"]:>8,}G {c["daily_profit"]:>9,}G')

    # ── Day 1 Analysis ──
    section(f'Day 1 Options (시작자금 {STARTING_MONEY:,}G)')
    for o in day1[:8]:
        profit = o['sell'] - o['cost']
        print(f'  {o["recipe_id"]} {o["name"]:<14} '
              f'{o["cost"]:>6,}G → {o["sell"]:>6,}G  (+{profit:>5,}G)  '
              f'{o["type"]}')

    # ── Ingredient Usage ──
    section('Ingredient Usage')
    sorted_usage = sorted(usage.items(), key=lambda x: -len(x[1]))
    overused = [(f, r) for f, r in sorted_usage if len(r) >= 4]
    unused = [i for i in analyzer.ingredients if i not in usage]
    single = [(f, r) for f, r in sorted_usage
              if len(r) == 1 and f in analyzer.ingredients]

    for fid, rids in overused:
        print(f'  ⚠ 과사용: {analyzer.food_name(fid)} ({fid}) — {len(rids)}개 레시피')
    for iid in unused:
        print(f'  ⚠ 미사용: {analyzer.food_name(iid)} ({iid})')
    if single:
        names = [analyzer.food_name(f) for f, _ in single]
        print(f'  ℹ 단일사용 ({len(single)}개): {", ".join(names)}')

    # ── Minigame Distribution ──
    section('Minigame Distribution')
    total = sum(len(r) for r in mg_dist.values())
    for mg in sorted(MINIGAME_NAMES):
        rids = mg_dist.get(mg, [])
        n = len(rids)
        pct = n / total * 100 if total else 0
        bar = '█' * int(pct / 2.5)
        flag = ' ⚠ 과집중' if pct > 30 else (' ⚠ 미사용' if n == 0 else '')
        print(f'  {mg} {MINIGAME_NAMES[mg]:<6} {n:>2} ({pct:>4.0f}%) {bar}{flag}')

    # ── Warnings ──
    section('Warnings')
    all_warns = dead + weight_issues

    # Dominant strategy check
    mains = [d for d in dishes if d['type'] == 'MAIN']
    if len(mains) >= 2:
        best, worst = mains[0], mains[-1]
        ratio = best['rpg'] / worst['rpg'] if worst['rpg'] > 0 else float('inf')
        if ratio > 1.5:
            all_warns.append(
                f'지배전략: {best["name"]} ({best["rpg"]:.0f}%/G) vs '
                f'{worst["name"]} ({worst["rpg"]:.0f}%/G) — {ratio:.1f}x 차이'
            )

    # Score 0 no-loss check
    loss_possible = any(d['sell_0'] < d['raw_cost'] for d in dishes)
    if not loss_possible:
        all_warns.append('score 0에서도 손실 없음 — 실패 페널티 부재')

    if all_warns:
        for w in all_warns:
            print(f'  ⚠ {w}')
    else:
        print('  ✓ No issues found')

    print()
    hr('═')


def print_chain(analyzer, recipe_id):
    """Print detailed chain for a specific recipe."""
    if recipe_id not in analyzer.recipes:
        print(f'Error: {recipe_id} not found')
        return

    price, raw, games, steps = analyzer.calc_recipe(recipe_id, 1.0)
    name = analyzer.food_name(analyzer.recipes[recipe_id]['output_id'])

    hr('═')
    print(f'  Chain Detail: {name} ({recipe_id})')
    hr('═')
    print()

    for i, step in enumerate(steps, 1):
        mg = step['minigame']
        mg_name = MINIGAME_NAMES.get(mg, '?')
        print(f'  Step {i}: {step["recipe_id"]} → {step["name"]} '
              f'[{mg} {mg_name}]  = {step["price"]:,}G')
        for fid, fname, w in step['raw_inputs']:
            ft = analyzer.food_type(fid)
            tag = '(P)' if ft == 'PROCESSING' else f'({analyzer.ingredients.get(fid, {}).get("tags", "?")})'
            print(f'         {fname} {tag}  w={w:.2f}')
        print()

    roi = (price - raw) / raw * 100 if raw else 0
    print(f'  Total: {raw:,}G → {price:,}G  '
          f'ROI {roi:.0f}%  미니게임 {games}회  ({roi/games:.0f}%/game)')
    hr('═')


def print_compare(analyzer):
    """Compare old uniform weight vs current weights."""
    hr('═')
    print('  Old (1.20 uniform) vs Current Weight System')
    hr('═')
    print()

    print(f' {"Name":<14} {"Type":<5} '
          f'{"Old@1.0":>9} {"New@1.0":>9} {"변화":>7}')
    hr('·')

    for rid, recipe in sorted(analyzer.recipes.items()):
        ft = analyzer.food_type(recipe['output_id'])
        if ft not in ('MAIN', 'SIDE'):
            continue

        new_price, raw, _, _ = analyzer.calc_recipe(rid, 1.0)

        # Simulate old system: all weights = 1.20
        old_price = _calc_old_price(analyzer, rid, 1.0)

        diff_pct = (new_price - old_price) / old_price * 100 if old_price else 0
        name = analyzer.food_name(recipe['output_id'])
        sign = '+' if diff_pct >= 0 else ''
        print(f' {name:<14} {ft:<5} '
              f'{old_price:>8,}G {new_price:>8,}G {sign}{diff_pct:>5.0f}%')

    print()
    hr('═')


def _calc_old_price(analyzer, recipe_id, score):
    """Calculate price with old uniform weight=1.20."""
    recipe = analyzer.recipes[recipe_id]
    total = 0.0
    for inp in recipe['inputs']:
        fid = inp['food_id']
        w = 1.20  # old uniform weight
        if fid in analyzer.output_to_recipe:
            sub = _calc_old_price(analyzer, analyzer.output_to_recipe[fid], score)
            total += sub * (1 + w * score)
        else:
            base = analyzer.ingredients.get(fid, {}).get('price', 0)
            total += base * (1 + w * score)
    return int(total)


# ---------------------------------------------------------------------------
# Entry Point
# ---------------------------------------------------------------------------
def main():
    parser = argparse.ArgumentParser(description='Game Balance Analysis Tool')
    parser.add_argument('--score', type=float, default=1.0,
                        help='Minigame score for analysis (0.0-1.0)')
    parser.add_argument('--chain', type=str, default=None,
                        help='Show detailed chain for a recipe (e.g. R009)')
    parser.add_argument('--compare', action='store_true',
                        help='Compare old (1.20 uniform) vs current weights')
    args = parser.parse_args()

    ingredients = load_ingredients()
    recipes = load_recipes()
    foods = load_foods()
    analyzer = BalanceAnalyzer(ingredients, recipes, foods)

    if args.chain:
        print_chain(analyzer, args.chain)
    elif args.compare:
        print_compare(analyzer)
    else:
        print_report(analyzer)


if __name__ == '__main__':
    main()
