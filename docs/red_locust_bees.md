# RedLocustBees 解析メモ

この資料は、BeeOverlay が可視化する `RedLocustBees` の state 0 維持・遷移条件をまとめた開発者向け解析メモです。
現在の対象ゲームバージョンは Lethal Company v73 系です。
ゲームの対応バージョンを更新する際は、バージョン別の資料を追加せず、このファイルの解析結果を新しいバージョン向けに置き換えます。
実装構成と表示の設計は [architecture.md](architecture.md) を参照してください。

## 対象と目的

- 対象: 現在は Lethal Company v73 系の `RedLocustBees`
- 調査目的: 蜂を state 0 から遷移させない場所を判断する
- 対象範囲: state 0 から state 1 および state 2 へ移る際の空間条件

## 観測するゲーム側のデータ

- `thisEnemyIndex`: 蜂ごとの安定した追跡用キー。
- `hive`: 現在の hive と、その `transform.position`。
- `eye`: 視線判定の始点。存在しない場合は蜂本体位置を代用する。
- `defenseDistance`: hive とローカルプレイヤー間の近接判定に使う距離。
- `lastKnownHivePosition`: hive 不明時の位置判定に使う記憶済み座標。
- `syncedLastKnownHivePosition`: private field。同期済みかを判定するため、Harmony の `AccessTools.FieldRefAccess` で読み取る。

`syncedLastKnownHivePosition` を読み取れない場合は、状態を false と見なさず「不明」として扱う必要があります。

## state 0 から state 1 への条件

### プレイヤー視認

`CheckLineOfSightForPlayer(360f, 16, 1)` は、蜂 eye を始点にローカルプレイヤーを視認できるかを確認します。
距離ゲートは 16u です。

- 視線判定と距離計算は、ローカルプレイヤーの実座標を使う。
- 描画上の視認線だけは、ちらつく赤線が視界中央を横切ることを抑えるため、プレイヤー側の端を 0.35u 下げる。
- `bee-hive` の表示は、プレイヤーが hive を拾う瞬間を `player ≒ hive` と見なすための事前予測であり、プレイヤー collider 自体を判定するものではない。

### hive 周辺の近接

ローカルプレイヤー本体と hive の距離を `RedLocustBees.defenseDistance` と比較します。
この判定はカメラ位置ではなくプレイヤー本体位置を使います。

### hive pickup proxy

蜂 eye から現在の hive 位置への linecast と距離を調べます。

- 16u 未満かつ linecast clear のとき、hive 位置で拾うプレイヤーを蜂が見られる可能性が高い。
- linecast blocked または 16u 以上のとき、この空間条件は満たされない。

## state 0 から state 2 への条件

state 0 から state 1 を避けることが主目的のため、state 2 側は `IsHiveMissing()` に関係する空間ゲートだけを扱います。
実際の遷移は hive 状態も含むゲーム側の条件に依存します。

### IsHiveMissing() の空間ゲート

`lastKnownHivePosition` を基準に、蜂 eye から次の条件を確認します。

- 4u 未満なら近距離ゲートに入る。
- 8u 未満かつ linecast clear なら視線ゲートに入る。
- `syncedLastKnownHivePosition` が false のときは評価しない。

`hive.isHeld` は意図的に可視化しません。
この資料とBeeOverlayの目的は、持っていると仮定したときに state 2 へ落ちうる位置を判断することです。

## 表示用の解釈

BeeOverlay は状態遷移そのものを決定せず、ゲーム側の空間条件を可視化します。

- `SEEN`: 視認できている、または対象の距離・linecast 条件を満たす。
- `blocked`: 視認できない、linecast が遮られている、または距離条件を満たさない。
- `INSIDE`: `defenseDistance` 内。
- `outside`: `defenseDistance` 外。
