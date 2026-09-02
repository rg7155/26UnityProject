"""AI 생성 스프라이트 반입 파이프라인.

ArtSource/Generated/Source/<Name>_Source.png (그린스크린 배경, 불투명)
  -> Assets/Art/Sprites/Generated/<Name>.png  (투명 배경, 규격 정규화)

규격 정본: .agents/skills/art-gen/references/art-spec.md

수동 조정을 없애는 것이 목적이다. v1은 생성물마다 여백이 달라 PPU 5종과
머티리얼 UV 3종을 인스펙터에서 손으로 맞췄고, 그게 부채로 남았다.
여기서 여백을 기계적으로 통일하면 PPU는 단일값, UV는 scale 1 / offset 0 으로 수렴한다.

사용:
    python tools/art_import.py                 # Source 전체 처리
    python tools/art_import.py Player Boss     # 이름으로 골라 처리
    python tools/art_import.py --dry-run       # 출력 없이 계산 결과만
"""

import argparse
import io
import sys
from datetime import datetime
from pathlib import Path

# Windows 기본 stdout 이 cp949 라 한글 로그가 깨진다.
try:
    sys.stdout.reconfigure(encoding="utf-8")
except AttributeError:
    pass

try:
    from PIL import Image
except ImportError:
    sys.exit("Pillow 가 필요하다:  pip install pillow")


ROOT = Path(__file__).resolve().parent.parent
SRC_DIR = ROOT / "ArtSource" / "Generated" / "Source"
OUT_DIR = ROOT / "Assets" / "Art" / "Sprites" / "Generated"
LOG_PATH = ROOT / "tools" / "art_import_log.md"

# 피사체가 프레임에서 차지하는 비율. art-spec.md 4절.
FILL_RATIO = 0.85

# --- 크로마키 (그린스크린) ---
# 배경을 검정이 아니라 녹색으로 두는 이유: 네온 아트는 어두운 내부 채움(dark core, 음영 패널)이
# 있어서 검정 배경으로 뽑으면 그 부분까지 투명해져 구멍이 뚫린다.
# 기존 9장도 실제로 그린스크린으로 만들어졌다 (소스 코너 픽셀이 #20E820 부근).
#
# 판정값 d = G - max(R, B).  녹색이 지배적일수록 크다.
KEY_HIGH = 90   # d 가 이 이상이면 완전 배경 (알파 0)
KEY_LOW = 30    # d 가 이 이하면 완전 피사체 (알파 255). 사이는 선형 램프 — 경계 안티에일리어싱 보존

# 바운딩박스 판정 임계값. 흐린 잔광에 박스가 끌려가지 않게 한다.
BBOX_THRESHOLD = 32

# 가로 정렬 기준으로 쓸 상단 영역 비율. 사람형 캐릭터의 머리에 해당한다.
# 무기가 한쪽으로 튀어나와도 이 영역은 몸통 위에 있어 중심이 밀리지 않는다.
HEAD_BAND = 0.30

# 생성 모델이 프레임마다 실루엣 크기를 바꿨는지 경고하는 기준.
# 크기 자동 보정은 바운스의 squash/stretch까지 지우므로 하지 않는다.
SIZE_VARIATION_LIMIT = 0.03


# 대상별 설정. 여기 없는 이름은 DEFAULT 를 쓴다.
#   frames  : 가로 스트립의 프레임 수 (1 = 정지 이미지)
#   size    : 프레임 한 장의 반입 해상도 (px)
#   symmetric: 좌우/상하 대칭 검사 대상 여부
#   humanoid : 좌우 반전 대상. 상단 영역 기준으로 가로 정렬한다 (무기 오프셋 보정)
#
# 키는 출력 파일명(확장자 제외)이다. 리스킨 대상은 기존 파일명을 그대로 쓴다 —
# 파일명이 바뀌면 .meta 의 guid 가 새로 발급되어 프리팹·머티리얼 참조가 전부 끊긴다.
# 그래서 신규 에셋만 새 이름을 갖고, 기존 7종은 Symmetric 접미사를 유지한다.
# (접미사는 guid 보존용 잔재다. v2 캐릭터는 대칭이 아니다 — art-spec.md 3절)
#
# 대칭 검사는 원형이어야 하는 오브젝트에만 건다. 캐릭터는 정면 뷰라 대칭이 아니고,
# 검사를 걸면 매번 무의미한 경고가 뜬다.
DEFAULT = dict(frames=1, size=512, symmetric=False, humanoid=False, force_frames=False)

TARGETS = {
    # A. 플레이어 — 스타일 기준점
    "PlayerEnergyCoreSymmetric":dict(frames=4, size=512,  symmetric=False, humanoid=True),
    "PlayerEnergyCoreMove":     dict(frames=4, size=512,  symmetric=False, humanoid=True),
    # 생성기가 2:1 캔버스에 4개의 세로 효과 셀을 배치한다. 이 대상만 레퍼런스의 시각적
    # 셀 수를 신뢰해 4등분한다. 공통 크롭 뒤에는 정사각 4프레임으로 정규화된다.
    "PlayerHoverThruster":       dict(frames=4, size=512,  symmetric=False, force_frames=True),

    # B. 보스
    "BossMonolithSymmetric":    dict(frames=4, size=1024, symmetric=False, humanoid=True),

    # C. 적 3종 (기존 파일명 유지 — guid 보존)
    "BasicEnemyDroneSymmetric": dict(frames=4, size=512,  symmetric=False),
    "FastEnemyDroneSymmetric":  dict(frames=4, size=512,  symmetric=False),
    "TankEnemyDroneSymmetric":  dict(frames=4, size=512,  symmetric=False),

    # D. 오브젝트 — 회전 대칭이어야 하는 것들만 검사한다
    "OrbiterEnergyBlade":       dict(frames=4, size=512,  symmetric=True),
    "Projectile":               dict(frames=1, size=512,  symmetric=True),
    "ProjectileRapid":          dict(frames=1, size=512,  symmetric=True),
    "BossRadialProjectile":     dict(frames=1, size=512,  symmetric=True),
    "TelegraphRing":            dict(frames=1, size=512,  symmetric=True),
    "TelegraphFill":            dict(frames=1, size=512,  symmetric=True),

    # E. 그 외
    "TreasureChestSciFi":       dict(frames=1, size=512,  symmetric=False),
}


def config_for(name):
    cfg = dict(DEFAULT)
    cfg.update(TARGETS.get(name, {}))
    return cfg


def chroma_key(img):
    """그린스크린 배경을 알파로 바꾸고 초록 번짐(spill)을 제거한다.

    despill 을 빼먹으면 피사체 외곽에 초록 테두리가 남는다. 네온 시안(#35D9F5)은
    G 채널이 원래 높아서 무턱대고 G 를 깎으면 색이 변하므로, 배경 경계에서만 깎는다.
    """
    img = img.convert("RGB")
    px = img.load()
    w, h = img.size
    out = Image.new("RGBA", (w, h))
    op = out.load()
    span = float(KEY_HIGH - KEY_LOW)

    for y in range(h):
        for x in range(w):
            r, g, b = px[x, y]
            d = g - (r if r > b else b)          # 녹색 지배도

            if d >= KEY_HIGH:
                op[x, y] = (0, 0, 0, 0)
                continue
            if d <= KEY_LOW:
                op[x, y] = (r, g, b, 255)
                continue

            # 경계 램프 — 안티에일리어싱된 외곽을 살린다
            a = int(255 * (KEY_HIGH - d) / span + 0.5)
            # despill: 이 구간에서만 G 를 R/B 최대치까지 끌어내린다
            cap = r if r > b else b
            op[x, y] = (r, cap if g > cap else g, b, a)
    return out


def resolve_frames(img, want):
    """파일의 가로세로비로 실제 프레임 수를 판정한다.

    N프레임 가로 스트립이면 비율이 N 근처다. 정지 1장이면 1 근처다.
    설정값과 어긋나면 파일 쪽을 믿는다 — 스트립을 만들기 전 단계에서
    정지 이미지를 N등분해 버리는 것이 가장 흔한 사고다.
    """
    ratio = img.size[0] / float(img.size[1])
    for n in (1, 2, 3, 4, 5, 6, 8):
        if abs(ratio - n) < 0.15 * n:
            return n if n != 1 else 1
    print(f"    경고: 가로세로비 {ratio:.2f} 가 어떤 프레임 수와도 맞지 않는다. "
          f"설정값 {want} 로 진행한다")
    return want


def split_strip(img, frames):
    """가로 스트립을 프레임 단위로 자른다."""
    if frames <= 1:
        return [img]
    w, h = img.size
    if w % frames != 0:
        # 생성 결과가 정확히 나누어떨어지지 않는 경우가 흔하다. 균등 분할로 근사한다.
        print(f"    경고: 폭 {w} 가 프레임 수 {frames} 로 나누어떨어지지 않는다. 균등 분할로 근사한다.")
    step = w / frames
    return [img.crop((int(round(i * step)), 0, int(round((i + 1) * step)), h))
            for i in range(frames)]


def union_bbox(frames):
    """전 프레임 공통 바운딩박스.

    프레임마다 따로 크롭하면 재생 시 피사체가 떨린다. 애니메이션 품질의 핵심이다.
    """
    boxes = []
    for f in frames:
        alpha = f.getchannel("A").point(lambda v: 255 if v >= BBOX_THRESHOLD else 0)
        box = alpha.getbbox()
        if box:
            boxes.append(box)
    if not boxes:
        return None
    return (min(b[0] for b in boxes), min(b[1] for b in boxes),
            max(b[2] for b in boxes), max(b[3] for b in boxes))


def frame_bbox(frame):
    """프레임 하나의 불투명 피사체 바운딩박스."""
    alpha = frame.getchannel("A").point(lambda v: 255 if v >= BBOX_THRESHOLD else 0)
    return alpha.getbbox()


def warn_size_variation(boxes):
    """프레임별 실루엣 크기 편차가 크면 경고한다. 크기를 자동 보정하지는 않는다."""
    boxes = [box for box in boxes if box]
    if len(boxes) <= 1:
        return

    widths = [box[2] - box[0] for box in boxes]
    heights = [box[3] - box[1] for box in boxes]
    width_variation = (max(widths) - min(widths)) / max(widths)
    height_variation = (max(heights) - min(heights)) / max(heights)
    if width_variation > SIZE_VARIATION_LIMIT or height_variation > SIZE_VARIATION_LIMIT:
        print(f"    경고: 프레임 실루엣 크기 편차가 3%를 초과한다 "
              f"(폭 {width_variation*100:.1f}%, 높이 {height_variation*100:.1f}%). "
              "자동 스케일 보정하지 않는다")


def head_center_x(frame, box):
    """상단 영역의 알파 무게중심 x. 캐릭터의 '몸통 중심' 근사값이다.

    bbox 중앙을 그대로 쓰면 안 되는 이유: 무기가 한쪽으로 튀어나오면 bbox가 그쪽으로
    늘어나 몸통이 반대로 밀린다. 그 상태로 좌우 반전하면 몸이 좌우로 점프한다.
    사람형 캐릭터는 머리가 몸통 바로 위에 있으므로, 무기가 거의 없는 상단 영역의
    무게중심을 가로 정렬 기준으로 쓴다.
    """
    x0, y0, x1, y1 = box
    band = max(1, int((y1 - y0) * HEAD_BAND))
    a = frame.crop((x0, y0, x1, y0 + band)).getchannel("A")
    px = a.load()
    w, h = a.size
    sx = tot = 0
    for y in range(0, h, 2):
        for x in range(0, w, 2):
            v = px[x, y]
            if v > BBOX_THRESHOLD:
                sx += x * v
                tot += v
    if tot == 0:
        return (x1 - x0) / 2.0
    return sx / tot


def fit_square(frame, box, size, center_x=None):
    """공통 박스로 크롭한 뒤 정사각 캔버스에 FILL_RATIO 로 앉힌다.

    center_x 를 주면 크롭 좌표계 기준 그 x 가 캔버스 가로 중앙에 오도록 배치한다.
    세로는 항상 중앙 정렬 — 바운스 애니메이션의 상하 이동을 보존해야 하므로
    프레임별로 다시 맞추지 않는다.
    """
    cropped = frame.crop(box)
    cw, ch = cropped.size
    target = int(size * FILL_RATIO)
    scale = min(target / cw, target / ch)
    nw, nh = max(1, int(round(cw * scale))), max(1, int(round(ch * scale)))
    cropped = cropped.resize((nw, nh), Image.LANCZOS)

    canvas = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    if center_x is None:
        ox = (size - nw) // 2
    else:
        # 몸통 중심을 캔버스 중앙에 둔다. 공통 크롭의 투명 여백은 캔버스 밖으로
        # 나가도 되지만 실제 피사체가 잘리지 않는 범위 안으로 제한한다.
        ox = int(round(size / 2.0 - center_x * scale))
        visible = frame_bbox(frame)
        if visible:
            left = max(0, visible[0] - box[0]) * nw // cw
            right_src = min(cw, visible[2] - box[0])
            right = (right_src * nw + cw - 1) // cw
            ox = max(-left, min(ox, size - right))
    canvas.paste(cropped, (ox, (size - nh) // 2))
    return canvas


def symmetry_score(frame):
    """좌우/상하 대칭 정도. 0 이면 완전 대칭, 클수록 어긋난다 (평균 알파 차, 0~255)."""
    a = frame.getchannel("A")
    w, h = a.size
    lr = a.transpose(Image.FLIP_LEFT_RIGHT)
    tb = a.transpose(Image.FLIP_TOP_BOTTOM)
    ap, lp, tp = a.load(), lr.load(), tb.load()
    total = 0
    for y in range(0, h, 4):          # 4px 간격 샘플링. 검사용이라 전수는 불필요
        for x in range(0, w, 4):
            total += abs(ap[x, y] - lp[x, y]) + abs(ap[x, y] - tp[x, y])
    n = ((h + 3) // 4) * ((w + 3) // 4) * 2
    return total / n if n else 0.0


def process(src_path, dry_run=False):
    name = src_path.stem
    if name.endswith("_Source"):
        name = name[:-len("_Source")]
    cfg = config_for(name)

    img = Image.open(src_path)

    # 설정은 '의도한' 프레임 수이고, 실제 프레임 수는 보통 파일의 가로세로비가 결정한다.
    # 다만 PlayerHoverThruster 는 생성기가 2:1 캔버스 안에 4개 셀을 배치하는 예외다.
    # 이 이름에서만 명시적으로 4등분해, 공통 크롭 뒤 정사각 프레임으로 정규화한다.
    frames = cfg["frames"] if cfg["force_frames"] else resolve_frames(img, cfg["frames"])
    if frames != cfg["frames"]:
        print(f"[{name}]  설정 frames={cfg['frames']} 이지만 가로세로비가 "
              f"{img.size[0]/img.size[1]:.2f} 라 frames={frames} 로 처리한다")
    cfg["frames"] = frames

    print(f"[{name}]  frames={cfg['frames']}  size={cfg['size']}")
    print(f"    원본 {img.size[0]}x{img.size[1]}  mode={img.mode}")

    rgba = chroma_key(img)
    frames = split_strip(rgba, cfg["frames"])
    frame_boxes = [frame_bbox(frame) for frame in frames]
    warn_size_variation(frame_boxes)
    box = union_bbox(frames)
    if box is None:
        print("    실패: 알파가 전부 비어 있다. 배경이 순수 녹색인지 확인할 것")
        return None
    if box == (0, 0, frames[0].size[0], frames[0].size[1]):
        print("    경고: 바운딩박스가 프레임 전체다. 배경이 크로마키로 안 잡혔다는 뜻이니 "
              "배경색을 확인할 것 (기대값: 밝은 녹색)")
    print(f"    공통 바운딩박스 {box}  (크롭 {box[2]-box[0]}x{box[3]-box[1]})")

    centers_x = [None] * len(frames)
    if cfg["humanoid"]:
        if len(frames) == 1:
            centers_x[0] = head_center_x(frames[0], box)
            print(f"    가로 정렬 기준(상단 {int(HEAD_BAND*100)}% 무게중심) "
                  f"x={centers_x[0]:.0f} / bbox 중앙 x={(box[2]-box[0])/2:.0f}")
        else:
            centers_x = [head_center_x(frame, frame_box or box)
                         + (frame_box[0] - box[0] if frame_box else 0)
                         for frame, frame_box in zip(frames, frame_boxes)]
            print(f"    가로 정렬 기준(프레임별 상단 {int(HEAD_BAND*100)}% 무게중심) "
                  f"x={', '.join(f'{cx:.0f}' for cx in centers_x)} "
                  f"/ bbox 중앙 x={(box[2]-box[0])/2:.0f}")
    fitted = [fit_square(frame, box, cfg["size"], cx)
              for frame, cx in zip(frames, centers_x)]

    if cfg["symmetric"]:
        score = symmetry_score(fitted[0])
        mark = "OK" if score < 12 else "경고 — 대칭이 어긋난다. 재생성 검토"
        print(f"    대칭 점수 {score:.1f}  {mark}")

    out = Image.new("RGBA", (cfg["size"] * len(fitted), cfg["size"]), (0, 0, 0, 0))
    for i, f in enumerate(fitted):
        out.paste(f, (i * cfg["size"], 0))

    out_path = OUT_DIR / f"{name}.png"
    if dry_run:
        print(f"    (dry-run) 출력 예정 {out_path.name}  {out.size[0]}x{out.size[1]}")
    else:
        OUT_DIR.mkdir(parents=True, exist_ok=True)
        out.save(out_path)
        print(f"    출력 {out_path}  {out.size[0]}x{out.size[1]}")

    return dict(name=name, src=str(src_path.relative_to(ROOT)),
                out=str(out_path.relative_to(ROOT)),
                src_size=f"{img.size[0]}x{img.size[1]}",
                out_size=f"{out.size[0]}x{out.size[1]}",
                frames=cfg["frames"], bbox=str(box))


def append_log(records):
    """변환 파라미터를 남긴다. 재현 가능성이 파이프라인의 요건이다."""
    stamp = datetime.now().strftime("%Y-%m-%d %H:%M")
    lines = [f"\n## {stamp}\n",
             f"FILL_RATIO={FILL_RATIO}  KEY={KEY_LOW}~{KEY_HIGH}  HEAD_BAND={HEAD_BAND}  BBOX_THRESHOLD={BBOX_THRESHOLD}\n",
             "\n| 대상 | 원본 | 출력 | 프레임 | 공통 바운딩박스 |\n",
             "|---|---|---|---|---|\n"]
    for r in records:
        lines.append(f"| {r['name']} | {r['src_size']} | {r['out_size']} | {r['frames']} | {r['bbox']} |\n")

    header = "" if LOG_PATH.exists() else "# 아트 반입 로그\n\n`tools/art_import.py` 가 자동 기록한다.\n"
    with io.open(LOG_PATH, "a", encoding="utf-8") as f:
        if header:
            f.write(header)
        f.writelines(lines)


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("names", nargs="*", help="처리할 대상 이름 (생략 시 전체)")
    ap.add_argument("--dry-run", action="store_true")
    args = ap.parse_args()

    if not SRC_DIR.exists():
        sys.exit(f"소스 폴더가 없다: {SRC_DIR}")

    sources = sorted(SRC_DIR.glob("*.png"))
    if args.names:
        wanted = {n.lower() for n in args.names}
        sources = [s for s in sources
                   if any(w in s.stem.lower() for w in wanted)]
    if not sources:
        sys.exit("처리할 소스가 없다")

    records = []
    for src in sources:
        rec = process(src, args.dry_run)
        if rec:
            records.append(rec)

    if records and not args.dry_run:
        append_log(records)
        print(f"\n{len(records)}건 처리. 로그: {LOG_PATH.relative_to(ROOT)}")


if __name__ == "__main__":
    main()
