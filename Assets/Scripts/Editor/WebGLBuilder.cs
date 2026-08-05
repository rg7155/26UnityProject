#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Build.Reporting;

// 해커톤 제출용 WebGL 빌드. 실행: Tools/Build/WebGL Build
//
// GitHub Pages 는 응답 헤더(Content-Encoding)를 설정할 수 없다. 압축 빌드를 그대로 올리면
// 브라우저가 압축 파일을 해제하지 못해 로딩 바에서 멈춘다. decompressionFallback 을 켜면
// 로더 JS 가 직접 해제하므로 헤더 없이도 실행된다 — Pages 배포의 필수 조건.
public static class WebGLBuilder
{
    // GitHub Pages 는 저장소 루트 또는 /docs 만 서빙한다. 다른 경로에 빌드하면 배포가 불가능하다.
    const string OutputPath = "docs";

    [MenuItem("Tools/Build/WebGL Build")]
    public static void Build()
    {
        if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.WebGL)
        {
            EditorUtility.DisplayDialog("플랫폼 전환 필요",
                "File > Build Profiles 에서 Web 을 선택하고 Switch Platform 을 먼저 실행하세요.\n" +
                "스프라이트 재임포트 때문에 수 분~수십 분 걸립니다. 완료 후 이 메뉴를 다시 실행하세요.",
                "확인");
            return;
        }

        ApplySettings();

        var scenes = new List<string>();
        foreach (var s in EditorBuildSettings.scenes)
            if (s.enabled) scenes.Add(s.path);

        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = scenes.ToArray(),
            locationPathName = OutputPath,
            target = BuildTarget.WebGL,
            targetGroup = BuildTargetGroup.WebGL,
            options = BuildOptions.None,
        });

        var sum = report.summary;
        if (sum.result == BuildResult.Succeeded)
            Debug.Log($"[WebGLBuilder] 빌드 성공 — {OutputPath} / {sum.totalSize / (1024 * 1024)}MB / {sum.totalTime}");
        else
            Debug.LogError($"[WebGLBuilder] 빌드 실패 — {sum.result} / 에러 {sum.totalErrors}건");
    }

    static void ApplySettings()
    {
        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
        PlayerSettings.WebGL.decompressionFallback = true;

        // 첫 빌드는 런타임 에러를 잡는 것이 목적이라 스택트레이스를 켠다. 용량·속도가 아쉬우면
        // 제출 직전 빌드에서 ExplicitlyThrownExceptionsOnly 로 내린다.
        PlayerSettings.WebGL.exceptionSupport = WebGLExceptionSupport.FullWithStacktrace;

        // 기본 템플릿은 캔버스를 540x960px 로 고정해 창보다 커지면 상단 HUD 가 잘린다.
        PlayerSettings.WebGL.template = "PROJECT:Portrait";

        PlayerSettings.WebGL.dataCaching = true;
        PlayerSettings.runInBackground = true;

        AssetDatabase.SaveAssets();
    }
}
#endif
