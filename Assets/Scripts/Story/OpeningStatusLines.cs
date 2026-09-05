// 타이틀 상태줄 문안. 오프닝 P5 의 「세는 건 내가 할게」가 여기서 회수된다.
//
// 별도 클래스로 뺀 이유: 한글 폰트 아틀라스를 이 문자열들에서 수집한다(KoreanFontGenerator).
// TitleScene 에 문자열 리터럴로 두면 폰트 생성기가 읽을 곳과 실제로 쓰는 곳이 갈라져,
// 문안을 고치고 폰트를 다시 굽지 않으면 새 글자가 빈칸으로 나온다.
public static class OpeningStatusLines
{
    // 구간은 기존 퀘스트 TIME TRAVELER 티어를 그대로 쓴다 — 새 카운터를 만들지 않는다.
    public const int MidTier  = 30;
    public const int HighTier = 150;

    public const string Low  = "기동 대기. 버퍼 5초.";
    public const string Mid  = "되감기 {0}회 기록. 너는 매번 처음이라고 하더라.";
    public const string High = "{0}회. 나는 아직 세고 있어.";

    public static string For(int rewinds)
    {
        if (rewinds < MidTier)  return Low;
        if (rewinds < HighTier) return string.Format(Mid, rewinds);
        return string.Format(High, rewinds);
    }
}
