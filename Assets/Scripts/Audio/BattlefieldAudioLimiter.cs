public sealed class BattlefieldAudioLimiter
{
    private readonly int _maxVoices;
    private int _activeVoices;

    public BattlefieldAudioLimiter(int maxVoices)
    {
        _maxVoices = maxVoices < 1 ? 1 : maxVoices;
    }

    public bool TryAcquire()
    {
        if (_activeVoices >= _maxVoices)
        {
            return false;
        }

        _activeVoices++;
        return true;
    }

    public void Release()
    {
        if (_activeVoices > 0)
        {
            _activeVoices--;
        }
    }
}
