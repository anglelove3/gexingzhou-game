namespace GeXingzhou.Domain;
public sealed record StoryAction(string Id,string ChoiceCode,string OpportunityId);
public sealed record TransitionResult(bool Applied,WorldSnapshot Next,string? ErrorCode=null);
