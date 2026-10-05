namespace GeXingzhou.Domain;
public enum SliceStage { FreeArrival, InvitationPending, InvitationResolved, CandyHeyPending, CandyHeyDelivered, SoupMeet, MemoryActive, MemoryReturned, SliceComplete }
public enum FlowState { Field, Dialogue, Phone, Transition, Memory, Paused }
public readonly record struct Position2(float X, float Y);
