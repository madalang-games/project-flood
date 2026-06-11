# 챕터 테마 기획서 (Chapter Theme Design)

> **상태:** 초기 설계안 (Initial Draft)
> **관련 문서:** `progression-system-design.md`, `game-design.md`, `ui-ux-lobby.md`

---

## 1. 개요

챕터 테마는 플레이어가 스테이지를 진행하면서 경험하는 시각적 분위기를 정의합니다.
각 챕터는 고유한 `bg_theme_id`를 가지며, 이 ID는 클라이언트의 `VisualService`가 런타임에 해당 테마 프리셋을 로드하는 키로 사용됩니다.

### 테마와 보드 스킨의 구분

| 개념 | 데이터 소스 | 제어 주체 |
|------|------------|----------|
| **챕터 테마** (`bg_theme_id`) | `chapter.csv` | 챕터 진행에 따라 자동 적용 |
| **보드 스킨** (`board_theme.csv`) | 플레이어 선택 | 플레이어가 직접 변경 |

---

## 2. 테마 정의 구조

### 배경 비주얼 요소

| 요소 | 설명 | 구현 방식 |
|------|------|----------|
| `theme_color_1` | 주조색 (배경 상단, 글로우 강조색) | `_ThemeColor1` 쉐이더 변수 |
| `theme_color_2` | 보조색 (배경 하단, 그라디언트 끝) | `_ThemeColor2` 쉐이더 변수 |
| `accent_color` | UI 강조색 (챕터 버튼 테두리, 스테이지 노드 펄스) | 머티리얼 인스턴스 Tint |
| `bg_sprite_key` | 배경 스프라이트 리소스 키 | `Resources/Sprites/Backgrounds/` |
| `particle_preset_key` | 부유 파티클 프리셋 키 | `Resources/Prefabs/Particles/` |
| `chapter_chest_skin` | 챕터 완료 상자 스프라이트 키 | `Resources/Sprites/UI/Chests/` |

### 스테이지 노드 요소

| 요소 | 설명 | 데이터 소스 |
|------|------|------------|
| `node_resource_key` | 스테이지 노드 배경 스프라이트 키 | `dynamic_resource.csv` (category=`StageNode`) |
| `node_frame_color` | 노드 테두리/프레임 색상 | `Image.color` |
| `node_fill_color` | 노드 배경 채우기 색상 | `Image.color` |
| `node_glow_color` | 펄스 링 글로우 색상 (현재 스테이지) | `UIScalePulse` tint |

> **`node_resource_key` 컨벤션:** `stage_node_ch_{chapter_id}`
> `chapter.csv` 컬럼 추가 없이 클라이언트가 `chapter_id`로 직접 유추.
> 스프라이트는 통합 스프라이트시트(`stage_node_sheet.png`)를 슬라이싱하여 사용. → §9 참조.

---

## 3. 기믹 진행표

| 챕터 | 테마 | 신규 도입 기믹 | 누적 기믹 |
|------|------|--------------|---------|
| 1 | 들판 | *(없음)* | 기본 셀 |
| 2 | 바다 | 장애물 셀 (Obstacle) | +장애물 |
| 3 | 숲 | 프로텍터 셀 (Protector) | +프로텍터 |
| 4 | 사막 | 코어 셀 (Core) | +코어 |
| 5 | 설원 | 프로텍터 + 코어 복합 | 복합 조합 심화 |
| 6 | 심해 | 보드 180° 회전 | +회전 |
| 7 | 동굴 | 텔레포트 셀 (Teleport) | +포탈 |
| 8 | 화산 | 컨베이어 벨트 셀 (Conveyor) | +이동 기믹 |
| 9 | 하늘 | 신규 기믹 TBD | 전 기믹 복합 등장 시작 |
| 10 | 오로라 | 신규 기믹 TBD | 고밀도 복합 |
| 11 | 우주 | 신규 기믹 TBD | 전 기믹 총동원 |

> 챕터 9~11 신규 기믹 후보: 중력 역전(Reverse Gravity), 색상 변이 셀(Color-Shift), 시한부 셀(Timed Cell).

---

## 4. 확정 챕터 상세 명세

### 4.1 챕터 1 — 들판 (Grassland)

| 항목 | 값 |
|------|----|
| `chapter_id` / `bg_theme_id` | 1 |
| 스테이지 범위 | 1 ~ 20 |
| 신규 기믹 | 없음 |

**배경 색상**

| 키 | Hex | 용도 |
|----|-----|------|
| `theme_color_1` | `#6BBF4E` | 초록 잔디, 배경 상단 |
| `theme_color_2` | `#C8E87A` | 연두, 배경 하단 |
| `accent_color` | `#F5D020` | 햇살 노랑, 노드 펄스 |
| 보조 하늘색 | `#A8D8F0` | 배경 상단 하늘 |

**스테이지 노드**

| 키 | 값 |
|----|----|
| `node_resource_key` | `stage_node_ch_1` |
| `node_frame_color` | `#8B6914` (따뜻한 갈색 나무) |
| `node_fill_color` | `#C8E87A` (연두) |
| `node_glow_color` | `#F5D020` (햇살 노랑) |
| 컨셉 | 나무 팻말 프레임, 모서리에 작은 꽃·잔디 장식 |

**기타**

| 요소 | 값 |
|------|----|
| `bg_sprite_key` | `bg_grassland` |
| `particle_preset_key` | `particles_dandelion` |
| `chapter_chest_skin` | `chest_grassland` |
| 권장 팔레트 | `2(Green), 3(Yellow), 5(Orange), 0(Red), 1(Blue), 6(Cyan)` |

---

### 4.2 챕터 2 — 바다 (Ocean)

| 항목 | 값 |
|------|----|
| `chapter_id` / `bg_theme_id` | 2 |
| 스테이지 범위 | 21 ~ 40 |
| 신규 기믹 | 장애물 셀 (Obstacle) |

**배경 색상**

| 키 | Hex | 용도 |
|----|-----|------|
| `theme_color_1` | `#1A6FA8` | 깊은 바다 파란색, 배경 상단 |
| `theme_color_2` | `#3BBFCE` | 청록색 물결, 배경 하단 |
| `accent_color` | `#00E5FF` | 밝은 시안, 노드 펄스 |
| 보조 해변색 | `#F5DEB3` | 배경 하단 모래 레이어 |

**스테이지 노드**

| 키 | 값 |
|----|----|
| `node_resource_key` | `stage_node_ch_2` |
| `node_frame_color` | `#0A4F7A` (딥 네이비) |
| `node_fill_color` | `#3BBFCE` (청록) |
| `node_glow_color` | `#00E5FF` (시안) |
| 컨셉 | 산호·조개껍데기 프레임, 작은 파도·불가사리 장식 |

**기타**

| 요소 | 값 |
|------|----|
| `bg_sprite_key` | `bg_ocean` |
| `particle_preset_key` | `particles_bubble` |
| `chapter_chest_skin` | `chest_ocean` |
| 권장 팔레트 | `1(Blue), 6(Cyan), 12(Teal), 4(Purple), 0(Red), 7(Pink)` |

---

## 5. 컨셉 챕터 명세 (ch3 ~ ch11)

> 확정 명세 아님. 착수 시 §4 형식으로 상세화 필요.

| ch | 테마 | `theme_color_1` | `theme_color_2` | `accent_color` | 노드 프레임 컨셉 | 신규 기믹 |
|----|------|----------------|----------------|---------------|----------------|---------|
| 3 | 숲 | `#2D4A1E` | `#4A6741` | `#AAFF44` | 이끼 낀 돌 석판, 담쟁이·버섯 | 프로텍터 |
| 4 | 사막 | `#C47A2A` | `#E8C87A` | `#FFA020` | 사암 석판, 선인장·모래 소용돌이 | 코어 |
| 5 | 설원 | `#B8D8F0` | `#F0F8FF` | `#87CEEB` | 얼음 결정 프레임, 눈송이·눈 더미 | 프로텍터+코어 |
| 6 | 심해 | `#0A1628` | `#0D2B45` | `#00FFCC` | 잠수함 해치, 발광 해파리 | 보드 회전 |
| 7 | 동굴 | `#2F2F3A` | `#1A1A2E` | `#9B59B6` | 크리스탈 박힌 돌 슬래브, 종유석 | 텔레포트 |
| 8 | 화산 | `#1C0A00` | `#3A0A00` | `#FF4500` | 화산암 프레임, 용암 균열·불씨 | 컨베이어 |
| 9 | 하늘 | `#4FC3F7` | `#E1F5FE` | `#FFD700` | 구름 테두리 플랫폼, 햇살 빔 | TBD |
| 10 | 오로라 | `#1A1A4E` | `#0D0D2B` | `#00FF88` | 극야 얼음 패널, 오로라 파동·별 | TBD |
| 11 | 우주 | `#0A0A12` | `#050510` | `#B266FF` | 홀로그램 패널, 성운·행성 실루엣 | TBD |

---

## 6. CSV 데이터 매핑

### chapter.csv `bg_theme_id` 할당

| chapter_id | bg_theme_id | theme_name | 스테이지 범위 |
|-----------|-------------|------------|-------------|
| 1 | 1 | grassland | 1 ~ 20 |
| 2 | 2 | ocean | 21 ~ 40 |
| 3 | 3 | forest | 41 ~ 60 |
| 4 | 4 | desert | 61 ~ 80 |
| 5 | 5 | snowfield | 81 ~ 100 |
| 6 | 6 | deep_sea | 101 ~ 120 |
| 7 | 7 | cave | 121 ~ 140 |
| 8 | 8 | volcano | 141 ~ 160 |
| 9 | 9 | sky | 161 ~ 180 |
| 10 | 10 | aurora | 181 ~ 200 |
| 11 | 11 | space | 201 ~ 220 |

> `board_theme.csv`의 `theme_id`와 **별도 ID 공간** 사용. 혼용 금지.

### dynamic_resource.csv StageNode 키

`resource_key` 패턴: `stage_node_ch_{N}` | `category`: `StageNode`
`sprite_path` 패턴: `Assets/Resources/Sprites/StageNodes/stage_node_ch_{N}.png`

> 슬라이싱된 개별 파일로 저장 (스프라이트시트에서 분리 후). `Resources.Load<Sprite>()` 로 런타임 로드.

---

## 7. 에셋 네이밍 규칙

| 에셋 유형 | 경로 패턴 |
|----------|----------|
| 스테이지 노드 스프라이트 | `Assets/Resources/Sprites/StageNodes/stage_node_ch_{N}.png` |
| 스테이지 노드 시트 (원본) | `Assets/Art/StageNodeSheet/stage_node_sheet.png` |
| 배경 스프라이트 | `Assets/Resources/Sprites/Backgrounds/bg_{theme}.png` |
| 파티클 프리팹 | `Assets/Resources/Prefabs/Particles/particles_{name}.prefab` |
| 챕터 상자 | `Assets/Resources/Sprites/UI/Chests/chest_{theme}.png` |

---

## 8. 구현 주의사항 (Hooks)

### Hook 1 — 런타임 DynamicResourceService 없음 ⚠️
`dynamic_resource.csv`는 현재 **에디터 전용** (`UIEditorSetup.cs`)에서만 사용됨.
런타임 `resource_key` → `Sprite` 로드 서비스 없음.
`stage_node_ch_N` 런타임 로드 위해 `DynamicResourceService` (DDOL 싱글톤) 신규 구현 필요.

```csharp
// 구현 방향 예시
public class DynamicResourceService : MonoBehaviour
{
    // Assets/Resources/ 이후 경로로 Resources.Load 호출
    public Sprite GetSprite(string resourceKey) { ... }
}
```

### Hook 2 — `StageNodeView.Bind()` 시그니처 변경 필요 ⚠️
현재: `Bind(int stageId, int stars, bool unlocked, bool isCurrent)`
추가 필요: `int chapterId` (또는 `string nodeResourceKey`) 파라미터.
`HomeTabView`가 풀에서 노드 꺼낼 때 챕터 ID 함께 전달 필요.

### Hook 3 — 오브젝트 풀 스프라이트 오염 ⚠️
풀 크기 = 15. 챕터 경계 스크롤 시 이전 챕터 노드가 재활용됨.
`Bind()` 호출 시 **항상** 노드 스프라이트 갱신. 조건부 업데이트 금지.

### Hook 4 — sprite_path 경로 형식 통일 필요 ⚠️
기존 `dynamic_resource.csv`에 `Assets/Sprites/...`와 `Assets/Resources/Sprites/...` 혼재.
신규 `stage_node_ch_N` 항목은 `Assets/Resources/Sprites/StageNodes/` 하위 통일.
`DynamicResourceService` 구현 시 `Assets/Resources/` prefix 제거 후 `Resources.Load` 호출.

### Hook 5 — chapter.csv 스키마 변경 없음 (컨벤션 채택) ✅
`node_resource_key` 컬럼 미추가. `$"stage_node_ch_{chapterId}"` 컨벤션으로 유추.
`info_generator` 재실행 불필요.

---

## 9. 스테이지 노드 이미지 생성 가이드

### 스프라이트시트 사양

| 항목 | 값 |
|------|----|
| 레이아웃 | 4열 × 3행 (총 12칸, 마지막 칸 비움) |
| 셀 크기 | 256 × 256 px |
| 전체 이미지 크기 | 1024 × 768 px |
| 배경 | 크로마키 순수 초록 `#00FF00` (투명도 없는 단색) |
| 텍스트 | 없음 (숫자·문자 금지) |
| 스타일 | 픽셀 아트, 2D flat, 그림자 없음 |

### 그리드 순서

```
[ch1 들판] [ch2 바다] [ch3 숲 ] [ch4 사막]
[ch5 설원] [ch6 심해] [ch7 동굴] [ch8 화산]
[ch9 하늘] [ch10 오로라] [ch11 우주] [빈 칸]
```

### 통합 이미지 생성 프롬프트

```
Pixel art sprite sheet. Drawn at 32x32 pixel resolution, scaled up 8x to 256x256.
Hard pixel edges only. No anti-aliasing. No smooth gradients. No blending.
NES / SNES era style. Maximum 8 flat colors per button.

4 columns x 3 rows grid. Each cell 256x256 pixels. Total image 1024x768 pixels.
Solid bright green (#00FF00) chromakey background outside buttons. No transparency.
No text, no numbers, no letters anywhere.

BUTTON STRUCTURE — identical layout for all 11 buttons:

  [outer shape]  Square with 2-pixel stepped corner cuts at all four corners.
                 220x220 pixels, centered in its 256x256 cell. Floats on green chromakey.

  [bezel]        Thin 4-pixel flat-color border. Themed pixel art decorations are
                 embedded IN the bezel or extend slightly OUTWARD beyond it into the
                 chromakey margin — like ornaments growing from the frame edge.
                 Decorations do NOT extend inward; they live on or outside the bezel line.
                 Corners of the bezel may have small accent sprites (flowers, crystals, etc.)
                 that slightly protrude outward past the button silhouette.

  [center panel] Everything inside the bezel.
                 Flat dark color — NO decoration, NO patterns, NO artwork.
                 This area will have stage number, lock icon, and stars overlaid at runtime.
                 Fill color should be dark enough for white text to be readable.

Grid order left-to-right, top-to-bottom:

Row 1:
[1] Grassland
  bezel: warm brown wood grain (#8B6914), pixel grass blades sprouting upward from the bottom bezel edge (extending outward), small pixel daisy at each bottom corner protruding outward
  center panel: dark forest green (#1A3A10)

[2] Ocean
  bezel: dark navy (#0A3A5A), pixel wave foam pixels sitting on top of the top bezel edge (extending outward), small pixel shell at each bottom corner protruding outward
  center panel: deep navy (#0A1E30)

[3] Forest
  bezel: dark bark brown (#2A1A0A), pixel ivy leaf pixels climbing along the left and right bezel edges extending outward, small pixel mushroom at bottom-left corner protruding outward
  center panel: very dark green (#0F200A)

[4] Desert
  bezel: sandstone (#A07040), pixel sand grain dots along the bottom bezel edge extending outward, small pixel cactus at bottom-right corner protruding outward
  center panel: dark tan (#3A2210)

Row 2:
[5] Snowfield
  bezel: icy steel blue (#4A7A9A), pixel icicle spikes hanging downward from the top bezel edge (extending outward), small pixel snowflake at each top corner protruding outward
  center panel: dark icy blue (#0A1E2E)

[6] Deep Sea
  bezel: near-black navy (#0A0F1E), pixel bubble dots floating above the top bezel edge extending outward, small pixel jellyfish at bottom-left corner protruding outward
  center panel: near-black (#050A14)

[7] Cave
  bezel: dark slate (#2A2A3A), pixel stalactite spikes pointing downward from top bezel edge (extending outward), small pixel crystal gem at each top corner protruding outward
  center panel: near-black (#0A0A12)

[8] Volcano
  bezel: charcoal black (#1A0A00), pixel lava drip blobs hanging from the bottom bezel edge (extending outward), pixel ember spark dots at corners protruding outward
  center panel: very dark red (#1A0500)

Row 3:
[9] Sky
  bezel: sky blue (#4A9ACA), pixel cloud puff blocks sitting on top and bottom bezel edges (extending outward), small pixel bird silhouette at top-right corner protruding outward
  center panel: dark sky blue (#0A2A3A)

[10] Aurora
  bezel: deep navy (#1A1A4A), pixel aurora shimmer pixels along the top bezel edge in green and purple (extending outward), single-pixel star dots scattered along all bezel edges extending outward
  center panel: near-black navy (#0A0A1E)

[11] Space
  bezel: void black (#0A0A0A), single-pixel star dots scattered along all bezel edges extending outward, small pixel planet circle at top-right corner protruding outward
  center panel: pure black (#050505)

[12] Empty cell — solid #00FF00 fill only, no artwork
```

### 슬라이싱 후 처리

1. AI 생성 이미지에서 그린스크린(`#00FF00`) 제거 → 투명 PNG 변환
2. 셀별 슬라이싱: Unity Sprite Editor → Grid By Cell Size (256×256) → 11개 슬라이스 생성
3. 각 슬라이스 이름: `stage_node_ch_1` ~ `stage_node_ch_11`
4. 개별 PNG 추출 후 `Assets/Resources/Sprites/StageNodes/` 에 배치
5. 원본 시트는 `Assets/Art/StageNodeSheet/stage_node_sheet.png` 보관 (재생성 대비)
