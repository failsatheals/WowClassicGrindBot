namespace Libs
{
    public enum UI_ERROR
    {
        NONE = 0,
        ERR_BADATTACKFACING = 1,
        ERR_SPELL_FAILED_S = 2,
        ERR_SPELL_OUT_OF_RANGE = 3,
        ERR_BADATTACKPOS = 4,
        ERR_AUTOFOLLOW_TOO_FAR = 5,
        ERR_CREATURE_NOT_SKINNABLE = 6, //Custom Spell Failed Error from Message
        ERR_CREATURE_NEEDS_LOOTED = 7, //Custom Spell Failed Error from Message
        ERR_SKILL_NOT_HIGH_ENOUGH = 8, //Custom Spell Failed Error from Message
        ERR_USE_LOCKED_WITH_SPELL_KNOWN_SI = 9,
    }
}