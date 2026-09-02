---
title: Sprite 슬라이스를 OnPostprocessTexture에 적용해 이전 세로 조각이 한 임포트 사이클 남음
tags: [unity, sprite, texture-importer, asset-postprocessor, slicing, animation, stale-import]
symptom: "4프레임 가로 스트립의 meta rect는 512x512로 정상인데 Project 창과 런타임에서는 이전 128px 세로 조각으로 보임"
commit: (pending)
files: [Assets/Scripts/Editor/GeneratedSpritePostprocessor.cs]
---

## 증상

512px 정지 이미지를 4프레임으로 잘못 나눈 이력이 있는 상태에서 2048×512 스트립으로 교체했다.
`.meta`의 네 `rect`는 512×512로 정상이지만 Project 창과 런타임에서는 헬멧·몸통·총이 세로 조각으로 나뉩다.

## 근본 원인

`TextureImporter.spritesheet`를 `OnPostprocessTexture`에서 설정했다. 현재 import의 Sprite 서브에셋이 이미
생성된 뒤에 새 rect가 `.meta`에만 저장되어, 메타와 실제 결과물이 한 import 사이클 어긋났다.

## 해결

`OnPreprocessTexture`에서 `GetSourceTextureWidthAndHeight`로 스트립을 판정하고, 원본 크기로
`SpriteMetaData[]`를 import 전에 설정한다. `OnPostprocessTexture` 슬라이싱은 제거하고 기존 에셋을 `Reimport`한다.

## 재인식 패턴 ⚠️

- `.meta`는 정상인데 Project 미리보기는 이전 슬라이스다 → 다음 import용 설정을 포스트프로세스에서 늦게 쓰는지 확인한다.
- 정지 512px → 스트립 2048px 교체 후 128px 세로 조각이 보인다 → 이전 정지본 폭의 stale Sprite 서브에셋을 의심한다.
