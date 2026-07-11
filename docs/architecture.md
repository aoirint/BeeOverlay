# BeeOverlay アーキテクチャ

BeeOverlay は、Lethal Company の `RedLocustBees` に関する state 0 の空間条件を、HUD と 3D ガイドで同じフレームに可視化する診断用MODです。
ゲーム側の現行バージョンに対する解析結果は [red_locust_bees.md](red_locust_bees.md) を参照してください。

## 構成

- エントリーポイント: `BeeOverlay/Plugin.cs` の `Plugin`。
- 更新契機: `HUDManager.Update` の Harmony postfix。
- 表示管理: `Overlay`。
- 蜂ごとのワールド表示: `BeeView`。

`Plugin.Awake()` が `Overlay` を作成し、Harmony patch を適用します。
`Overlay.Tick()` は毎フレーム、HUDの準備、蜂の列挙、ワールドガイドの描画、左上ステータスの再構築を行います。

## HUD のライフサイクル

`Overlay` は `HUDManager.Instance.HUDContainer` 配下に左上ステータスUIを作成します。
HUDコンテナが存在しない、またはシーン遷移で親が変わった場合は、古い表示を隠して再作成します。

- `RedLocustBees` は `thisEnemyIndex` 順に並べる。
- HUDの `bee:*` 番号は、ソート後の 1 始まりの連番。
- `BeeView` の内部キーとログは、追跡しやすさのため `thisEnemyIndex` を使う。
- ステータスはキャッシュせず、毎フレーム組み立てる。これにより despawn や不完全な navigation data による古い行が残らない。

## 3D ガイド

hive を持つ蜂について、state 0 維持に関係する空間条件だけを描画します。

| 対象 | 表示 | 色 |
| --- | --- | --- |
| hive | `defenseDistance` の水平円とマーカー | 緑 |
| `lastKnownHivePosition` | マーカー、4u円、8u円、蜂 eye からの線 | 青系 |
| bee eye | 視認距離の水平円とマーカー | 黄色 |
| ローカルプレイヤー | bee eye からの視認線とマーカー | 赤 |
| 現在の hive への視線 | bee eye から hive への線 | 白、条件外はグレー |
| inactive / blocked | 線 | グレー |

マーカーは、地形・hive メッシュ・蜂本体に埋もれないよう、サンプル座標から少し上に描画します。
ワールドマーカーの collider は削除し、ゲームの物理、raycast、近傍 collider を調べる他MODの挙動を変えないようにします。

## 左上ステータス

ステータスは次の形式です。

```text
Bee Overlay | bees=2
bee:1  bee-player=6.20u/SEEN  hive-player=5.12u/INSIDE  bee-hive=7.10u/SEEN  bee-knownHive=3.82u/SEEN
bee:2  bee-player=10.85u/blocked  hive-player=8.34u/outside  bee-hive=9.44u/blocked  bee-knownHive=9.80u/blocked
```

- `bee-player`: 蜂 eye からローカルプレイヤー本体までの距離と、ローカルプレイヤーを見ているか。
- `hive-player`: hive からローカルプレイヤー本体までの距離と、`defenseDistance` 内か。
- `bee-hive`: 蜂 eye から現在の hive までの距離と、16u 未満かつ linecast clear か。
- `bee-knownHive`: 蜂 eye から `lastKnownHivePosition` までの距離と、linecast clear か。

HUD文字色はワールドの固有色に合わせます。`bee-player` は赤、`hive-player` は緑、`bee:*` は黄色、`bee-hive` は白、`bee-knownHive` は青です。

## ログ方針

通常の heartbeat ログは出しません。
距離、視認、state 0 維持に関係する情報は、HUD と 3D表示へ集約します。
ログが必要な場合は、調査対象を絞った一時ログとして追加します。

## 動作確認

ビルドは次のコマンドで確認します。

```powershell
dotnet build BeeOverlay.sln -c Release
```

ゲーム内では次を確認します。

- 左上に `Bee Overlay | bees=N` が表示される。
- hive 周囲に緑の `defenseDistance` 円が表示される。
- 蜂とローカルプレイヤーの線は、視認中は赤、非視認時はグレーになる。
- 蜂 eye から hive への緑またはグレーの線が表示される。
- `lastKnownHivePosition` の青点、4uの中間青円、8uの薄い青円が表示される。
- `IsHiveMissing()` の空間ゲートが成立しうるとき、蜂 eye から `lastKnownHivePosition` への線が青になる。
