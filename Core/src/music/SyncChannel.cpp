#include "SyncChannel.h"
#include <iostream>
#include <numeric>
#include <stdexcept>

namespace ojgl {

SyncChannel::SyncChannel(int numNotes, int minNote, int channel)
    : numNotes(numNotes)
    , channel(channel)
    , _minNote(minNote)
{
    _lastTimePerNote.resize(numNotes);
    _timesPerNote.resize(numNotes);
    _totalHitsPerNote.resize(numNotes);
}

void SyncChannel::pushNote(int absoluteNote, timer::Milliseconds time)
{
    _timesPerNote[absoluteNote - _minNote].push(time);
}

void SyncChannel::tick(timer::Milliseconds currentTime)
{
    _currentTime = currentTime;
    for (int note = 0; note < numNotes; note++) {
        std::queue<timer::Milliseconds>& s = _timesPerNote[note];
        while (!s.empty() && s.front() <= _currentTime) {
            _lastTimePerNote[note] = _currentTime;
            _totalHitsPerNote[note]++;
            s.pop();
        }
    }
}

timer::Milliseconds SyncChannel::getTimeToNext(int relativeNote) const
{
    const std::queue<timer::Milliseconds>& times = _timesPerNote[relativeNote];
    if (times.empty()) {
        return timer::Milliseconds(std::numeric_limits<long long>::max());
    }
    return times.front() - _currentTime;
}

timer::Milliseconds SyncChannel::getTimeSinceLast(int relativeNote) const
{
    if (_totalHitsPerNote[relativeNote] == 0) {
        return timer::Milliseconds(std::numeric_limits<long long>::max());
    }
    return _currentTime - _lastTimePerNote[relativeNote];
}

int SyncChannel::getTotalHitsPerNote(int relativeNote) const
{
    return _totalHitsPerNote[relativeNote];
}

int SyncChannel::getTotalHits() const
{
    return std::accumulate(_totalHitsPerNote.begin(), _totalHitsPerNote.end(), 0);
}
} //namespace ojgl
