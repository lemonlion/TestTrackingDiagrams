namespace TestTrackingDiagrams.Extensions.Redis;

public enum RedisOperation
{
    Get,
    Set,
    Delete,
    KeyExists,
    Expire,
    HashGet,
    HashSet,
    HashDelete,
    HashGetAll,
    ListPush,
    ListRange,
    SetAdd,
    SetMembers,
    Increment,
    Decrement,
    Publish,
    Other
}
