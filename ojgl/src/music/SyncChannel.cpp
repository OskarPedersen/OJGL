#include "SyncChannel.h"
#include "utility/Log.h"
#include "utility/OJstd.h"

namespace ojgl {

SyncChannel::SyncChannel(int numNotes, int minNote, int channel)
    : numNotes(numNotes)
    , channel(channel)
    , _minNote(minNote)
{
    for (int i = 0; i < numNotes; i++) {
        _lastTimePerNote.push_back(Duration(0));
        _timesPerNote.push_back(ojstd::vector<Duration>());
        _totalHitsPerNote.push_back(0);
    }
}

void SyncChannel::pushNote(int absoluteNote, Duration time)
{
    _timesPerNote[absoluteNote - _minNote].push_back(time);
}

void SyncChannel::tick(Duration currentTime)
{
    _currentTime = currentTime;
    for (int note = 0; note < numNotes; note++) {
        ojstd::vector<Duration>& s = _timesPerNote[note];
        while (!s.empty() && s[0] <= _currentTime) {
            _lastTimePerNote[note] = _currentTime;
            _totalHitsPerNote[note]++;
            s.erase(s.begin());
        }
    }
}

Duration SyncChannel::getTimeToNext(int relativeNote) const
{
    const ojstd::vector<Duration>& times = _timesPerNote[relativeNote];
    if (times.empty()) {
        return Duration::maximum();
    }
    return times[0] - _currentTime;
}

Duration SyncChannel::getTimeSinceLast(int relativeNote) const
{
    if (_totalHitsPerNote[relativeNote] == 0) {
        return Duration::maximum();
    }
    return _currentTime - _lastTimePerNote[relativeNote];
}

Duration SyncChannel::getTimeSinceAnyNote() const
{
    Duration min = Duration::maximum();
    for (int i = 0; i < _lastTimePerNote.size(); i++) {
        min = Duration::min(min, _currentTime - _lastTimePerNote[i]);
    }

    return min;
}

int SyncChannel::getTotalHitsPerNote(int relativeNote) const
{
    return _totalHitsPerNote[relativeNote];
}

int SyncChannel::getTotalHits() const
{
    return ojstd::accumulate(_totalHitsPerNote.begin(), _totalHitsPerNote.end(), 0);
}
} //namespace ojgl
