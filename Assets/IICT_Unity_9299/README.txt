国際情報工科自動車大学校 3Dモデル Unityパッケージ

【中身】
Assets/Editor/IICTBuildingBuilder.cs   ... パーツごとに編集できる建物を作るスクリプト
Assets/IICT_Models/iict_building.obj   ... 1つにまとまったモデル(見るだけ用)
Assets/IICT_Models/iict_building.mtl   ... 上のマテリアル

単位=メートル / 原点(0,0,0)=エレベーター中心の地面の高さ
(UnityはX軸の向きがウェブ表示と逆のため、スクリプト側で反転済み)

【入れ方】
1. Unity Hub で「3D」プロジェクトを新規作成(または既存プロジェクトを開く)。
2. このZIPを解凍し、中の「Assets」フォルダを、プロジェクトのフォルダにドラッグして
   上書き(マージ)する。もしくはUnityのProjectウィンドウに Editor と IICT_Models を
   ドラッグ&ドロップする。
   ※ Editorフォルダは必ず「Assets/Editor」に置く(別の場所だとメニューが出ない)。
3. 少し待つ(右下のコンパイルの回転マークが消えるまで)。上のメニューバーに「Tools」が出る。

【A. パーツごとに編集したいとき(おすすめ)】
1. メニュー Tools > IICT > Build Building を実行。
2. Hierarchy に IICT_Building ができる。
   階(1F/2F/3F/RF/Misc) > 種類(Wall/Floor/Ceiling...) > パーツ の順に並ぶ。
3. パーツをクリックし、Inspector の Transform で Position / Rotation / Scale を直接変更。
4. 編集が終わったら IICT_Building を Project にドラッグして Prefab 化して保存。
   ※ もう一度 Build Building を実行すると作り直しになり、手で直した分は消える。

【B. 見るだけのとき】
1. Assets/IICT_Models/iict_building.obj を Scene にドラッグ。
2. 壁が暗い/裏返しに見えるときは、obj を選択 > Inspector の Model タブ >
   Normals を「Calculate」にして Apply。

【困ったとき】
- Tools が出ない → Assets/Editor にあるか確認。Console(Window > General > Console)の
  赤いエラーを確認。
- 全部ピンク色 → URP/HDRPプロジェクトでマテリアルが合っていない。
  Edit > Rendering > Materials > Convert Selected Built-in Materials to URP を実行。
- ガラスが不透明 → そのマテリアルの Surface Type(Rendering Mode)を Transparent にする。
- 建物が大きすぎる/小さすぎる → 縮尺は仮定(図面1px=0.05m)。IICT_Building の Scale で調整。

【Build Building しても古い建物が出るとき】
1. Assets の中に IICTBuildingBuilder.cs が2つ以上ないか確認(「IICTBuildingBuilder 1.cs」など)。
   同じ名前のクラスが2つあるとコンパイルエラーになり、古いスクリプトのまま動きます。
   古いファイルを消して、Assets/Editor には新しい1つだけにする。
2. Console(Window > General > Console)に赤いエラーがないか確認。エラーがあると新しい版に切り替わりません。
3. 新しい版なら、Build Building 実行後に Console に
   「IICT building built. parts=... script version ...」と出ます(出なければ古い版のまま)。
4. 先に Tools > IICT > Delete Building を実行すると、古い IICT_Building を全部消せます
   (Hierarchyで手動で消してもOK。Prefab化したものは別途消す)。
