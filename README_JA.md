# ComicPlate

[中文 README](README.md) / [English README](README_EN.md)

ComicPlate は Windows と macOS 向けの軽量なローカル漫画リーダーです。C# と Avalonia で作られています。

ローカルフォルダーや漫画アーカイブを読みやすい本として開き、読書位置を復元します。ユーザーの元ファイルは変更しません。

**現在の公開バージョン:** 1.1.2

![ComicPlate screenshot](artworks/SamplePhoto.jpg)

スクリーンショット内のサンプル漫画ページは David Revoy による *Pepper&Carrot* で、CC BY 4.0 ライセンスのもとで使用しています。

## 状態

1.0.0 は最初の完成版の公開リリースです。

現在はインストーラーではなく、自己完結型ビルドとして配布します。Windows と macOS のインストーラー、署名、より整ったリリース手順は今後のバージョンで改善します。

## 対応プラットフォーム

現在のリリース対象：

* Windows x64
* macOS Apple Silicon

## 機能

* ローカルフォルダー、ZIP / CBZ、RAR / CBR アーカイブを開く。
* 単一画像を軽量プレビューとして開く。
* 単ページと見開き表示。
* 右から左 / 左から右の読書方向。
* キーボード、ボタン、ホイール、ドラッグで操作できる横方向の連続読書ストリップ。
* 現在のコンテナ内を移動するための Context Shelf。
* Continue Reading と本ごとの読書位置復元。
* 複数ウィンドウ、フルスクリーン、フルスクリーン時のツールバー自動非表示。
* 明るいテーマ、暗いテーマ、読書向けテーマの内蔵プリセット。
* 中国語、英語、日本語 UI。
* Windows のファイル関連付けと Explorer 右クリックメニューを明示的に設定する入口。

## スコープ

ComicPlate はリーダーであり、漫画ライブラリ管理ツールではありません。

ユーザーのコンテンツを読み取り、ページ一覧を作り、表示し、設定、セッション、読書進捗、ログ、キャッシュなど ComicPlate 自身の状態だけを保存します。

読書位置、ウィンドウ状態、言語とテーマ設定を保存できます。また、ユーザーが明示的に選んだ場合に限り、対応形式のファイル関連付けや右クリックメニューを登録できます。

ComicPlate はユーザーの漫画ファイルを削除、移動、リネーム、上書き、編集してはいけません。

## 対応形式

ComicPlate が開けるもの：

* フォルダー内の画像
* `.zip` / `.cbz`
* `.rar` / `.cbr`
* `.jpg` / `.jpeg`
* `.png`
* `.webp`
* `.bmp`
* `.gif` は最初のフレームのみ

現在のスコープ外：

* PDF
* EPUB / MOBI
* 動画 / 音声
* ネストしたアーカイブ
* 7z / CB7
* メタデータ管理
* ライブラリ全体のスキャン
* ファイル編集やファイル管理操作

## UI モデル

ComicPlate は起動時に軽量な入口画面を表示し、その後リーダーウィンドウへ移動します。

リーダーウィンドウには左側の Context Shelf、中央の Reader Stage、下部の進捗バーがあります。Shelf は現在のコンテナ内の近くの項目へ移動するためだけのもので、本棚ではありません。

設定ウィンドウは ComicPlate 自身の動作だけを扱います。言語、テーマ、読書設定、データフォルダー、ファイル関連付け、ショートカット表示などが対象で、ファイル管理は行いません。

## ソースから実行

必要なもの：

* .NET SDK

```bash
dotnet restore
dotnet run --project src/ComicPlate.App
```

Debug 構成：

```bash
dotnet run --project src/ComicPlate.App -c Debug
```

## ビルド

基本的な Release publish：

```bash
dotnet publish src/ComicPlate.App -c Release
```

macOS app bundle スクリプト：

```bash
bash scripts/package-macos-app.sh
```

Release 出力は自己完結型ビルドです。ビルド出力は Git に入れないでください。`artifacts/`、`publish/`、またはその他の ignore 済み出力フォルダーを使います。

## プロジェクト構成

```text
src/ComicPlate.App             Avalonia UI、ウィンドウ、ビュー、ViewModel
src/ComicPlate.Core            Book、Page、読書状態、ソート、ドメインルール
src/ComicPlate.Infrastructure  ファイルシステム、アーカイブ、永続化、プラットフォームサービス
tests/                         テスト
platform/                      プラットフォーム別ファイル
scripts/                       ビルドとパッケージングスクリプト
```

## アーキテクチャ

ComicPlate は小さな App / Core / Infrastructure の分割を使います。

```text
App             Avalonia UI、ウィンドウ、ビュー、ViewModel
Core            Book、Page、読書状態、ソート、ドメインルール
Infrastructure  ファイルシステム、アーカイブ、永続化、プラットフォームサービス
```

リーダーは本全体を一度にデコードするべきではありません。画像デコード、キャッシュ、メモリ動作は後からの仕上げではなく、読書体験の中核です。

## ロードマップ

近い作業：

* Windows と macOS のリリースパッケージングを整える。
* ファイル関連付け、署名、プラットフォーム統合を改善する。
* 画像デコード、キャッシュ、メモリ動作を引き続き改善する。
* 実際の使用感に合わせて読書操作と設定項目を絞り込む。

## 境界

ComicPlate は PDF リーダー、EPUB リーダー、画像エディター、メタデータエディター、一括リネームツール、ファイルマネージャー、完全な漫画ライブラリ管理ツールではありません。

## コントリビューション

issue は歓迎します。特に、読めないアーカイブ、ページ順の誤り、画像デコード失敗、見開きレイアウト、読書方向、プラットフォーム差異、メモリ問題、性能問題に関する報告は助かります。

大きな機能 PR は、先に issue で相談してください。

中心ルール：ComicPlate はユーザーの漫画ファイルを変更してはいけません。また、漫画ライブラリ管理ツールになってはいけません。

## License

See [LICENSE](LICENSE).
