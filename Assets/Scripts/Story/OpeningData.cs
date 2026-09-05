using UnityEngine;

// 오프닝 시퀀스 전체 정의. Resources/Story/Opening.asset 하나만 존재한다.
// 에셋을 고치면 대사·타이밍이 바뀌므로 코드 수정 없이 문안을 조정할 수 있다.
//
// 페이지 수와 대사 수는 기획 규격이다 — 5페이지 / 13줄 / 176자 / 약 28초.
// 이보다 길어지면 스킵률이 오른다. 늘릴 때는 총 재생 시간을 다시 계산할 것.
[CreateAssetMenu(fileName = "Opening", menuName = "Game/Opening")]
public class OpeningData : ScriptableObject
{
    public OpeningPage[] pages;

    [Header("타이밍")]
    // 타자기 속도. 30cps 는 한글 기준 "읽으면서 따라갈 수 있는" 상한이다.
    // 더 빠르면 타자기 효과 자체가 안 보이고, 더 느리면 탭으로 건너뛰게 된다.
    public float charsPerSecond = 30f;

    // 한 줄이 다 찍힌 뒤 자동으로 다음 줄로 넘어가기까지의 시간.
    // 탭하면 이 시간을 기다리지 않는다.
    public float autoAdvanceDelay = 2.2f;

    // 페이지가 바뀔 때 배경 크로스페이드 시간.
    public float pageFadeDuration = 0.35f;

    public int TotalLines
    {
        get
        {
            if (pages == null) return 0;
            int n = 0;
            foreach (OpeningPage page in pages)
                if (page != null && page.lines != null) n += page.lines.Length;
            return n;
        }
    }
}
