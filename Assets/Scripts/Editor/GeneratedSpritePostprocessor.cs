using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

// Assets/Art/Sprites/Generated/ 아래 PNG 의 임포트 설정을 자동 적용한다.
//
// 왜 필요한가: 기존 7장은 PPU 를 인스펙터에서 손으로 맞췄고 그 결과가 100/577/700/800/1150 으로
// 제각각이다. 생성 이미지마다 여백이 달랐기 때문인데, art_import.py 가 여백을 정규화하면서
// 그 이유가 사라졌다. 이제 PPU 는 규격당 하나면 된다.
//
// 규격 정본: .agents/skills/art-gen/references/art-spec.md
public class GeneratedSpritePostprocessor : AssetPostprocessor
{
    const string TargetDir = "Assets/Art/Sprites/Generated/";

    // 반입본은 프레임 한 장이 512(전투) 또는 1024(보스)다. art_import.py 의 size 와 짝을 이룬다.
    // PPU 를 프레임 크기와 같게 두면 스프라이트 1장 = 월드 1유닛이 되어 크기 계산이 단순해진다.
    // 개별 크기 조정은 프리팹 Transform 이 아니라 이 값으로 한다 —
    // Transform Scale 은 Collider 에도 영향을 준다(에디터_설정.md "크기 조절 원칙").
    const float DefaultPPU = 512f;
    // 기존 1254px / 577PPU 크기를 보존한다: 512 / (1254 / 577) = 235.5853, 정수 PPU로 반올림.
    const float PlayerPPU = 236f;
    // Projectile 프리팹의 0.2 스케일과 Collider는 유지한다. 512 PPU보다 512 / 192 = 2.67배 크게 보인다.
    const float ProjectilePPU = 192f;

    // 가로 스트립의 프레임 수. art_import.py 의 TARGETS 와 일치해야 한다.
    // 여기 없으면 단일 스프라이트로 본다.
    static readonly Dictionary<string, int> FrameCounts = new Dictionary<string, int>
    {
        { "PlayerEnergyCoreMove",      4 },
        { "PlayerHoverThruster",        4 },
        { "BossMonolithSymmetric",     4 },
        { "OrbiterEnergyBlade",        4 },
        { "BasicEnemyDroneSymmetric",  4 },
        { "FastEnemyDroneSymmetric",   4 },
        { "TankEnemyDroneSymmetric",   4 },
    };

    void OnPreprocessTexture()
    {
        if (!assetPath.StartsWith(TargetDir)) return;

        TextureImporter importer = (TextureImporter)assetImporter;
        string name = System.IO.Path.GetFileNameWithoutExtension(assetPath);
        bool isPlayer = name == "PlayerEnergyCoreSymmetric"
            || name == "PlayerEnergyCoreMove"
            || name == "PlayerHoverThruster";

        importer.textureType = TextureImporterType.Sprite;
        importer.spritePixelsPerUnit = isPlayer
            ? PlayerPPU
            : name == "Projectile"
                ? ProjectilePPU
                : DefaultPPU;
        importer.filterMode = FilterMode.Bilinear;
        importer.mipmapEnabled = false;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.alphaSource = TextureImporterAlphaSource.FromInput;
        importer.alphaIsTransparency = true;
        importer.sRGBTexture = true;

        int frames;
        int sourceWidth;
        int sourceHeight;
        importer.GetSourceTextureWidthAndHeight(out sourceWidth, out sourceHeight);
        float aspectRatio = sourceWidth / (float)sourceHeight;
        bool isStrip = FrameCounts.TryGetValue(name, out frames)
            && frames > 1
            && Mathf.Abs(aspectRatio - frames) < 0.15f * frames;

        if (isStrip)
        {
            int frameWidth = sourceWidth / frames;
            if (frameWidth * frames != sourceWidth)
            {
                Debug.LogError($"[GeneratedSpritePostprocessor] {name}: 폭 {sourceWidth} 가 " +
                               $"프레임 수 {frames} 로 나누어떨어지지 않는다. art_import.py 출력을 확인할 것");
            }
            else
            {
                SpriteMetaData[] slices = new SpriteMetaData[frames];
                for (int i = 0; i < frames; i++)
                {
                    SpriteMetaData meta = new SpriteMetaData();
                    meta.name = $"{name}_{i}";
                    meta.rect = new Rect(i * frameWidth, 0, frameWidth, sourceHeight);
                    meta.alignment = (int)SpriteAlignment.Center;
                    meta.pivot = new Vector2(0.5f, 0.5f);
                    slices[i] = meta;
                }

#pragma warning disable 618   // spritesheet 는 deprecated 지만 Unity 6 에서 여전히 유일한 배치 슬라이싱 경로다
                importer.spritesheet = slices;
#pragma warning restore 618
            }
        }

        // 프레임당 512px, 보스는 1024px를 유지한다. Single은 WebGL 용량을 위해 1024로 제한한다.
        importer.maxTextureSize = isStrip
            ? (name == "BossMonolithSymmetric" ? 4096 : 2048)
            : 1024;

        importer.spriteImportMode = isStrip
            ? SpriteImportMode.Multiple
            : SpriteImportMode.Single;
    }
}
