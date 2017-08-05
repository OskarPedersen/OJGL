#pragma once
#include <vector>

class SyncChannel {
public:
    SyncChannel();
    SyncChannel(int numNotes, int minNote, int channel, double decay = 0.95);
    ~SyncChannel();
    void tick();
    void setVelocity(int absoluteNote, double velocity);
    double getNoteVelocity(int relativeNote) const;
    double getTotalNoteVelocity() const;
    int getTotalHitsPerNote(int absoluteNote) const;
    int getTotalHits() const;
    int numNotes;
    int channel;

private:
    double _decay;
    int _minNote;
    std::vector<double> _currentNoteVelocities;
    std::vector<int> _totalHitsPerNote;
};
