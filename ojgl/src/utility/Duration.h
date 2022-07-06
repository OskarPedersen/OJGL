#pragma once

namespace ojgl {

class Duration {

    friend class Timepoint;

public:
    Duration(long duration)
        : _duration(duration)
    {
    }

    Duration() = default;

    template <typename T = float>
    T toSeconds() const { return static_cast<T>(this->_duration) / 1000; }

    template <typename T = float>
    T toMilliseconds() const { return static_cast<T>(this->_duration); }

    Duration operator-() const { return Duration(-this->_duration); }
    Duration operator-(const Duration& other) const { return Duration(this->_duration - other._duration); }
    Duration operator+(const Duration& other) const { return Duration(this->_duration + other._duration); }
    bool operator<(const Duration& other) const { return this->_duration < other._duration; }
    bool operator<=(const Duration& other) const { return this->_duration <= other._duration; }
    bool operator>(const Duration& other) const { return this->_duration > other._duration; }
    bool operator>=(const Duration& other) const { return this->_duration >= other._duration; }
    bool operator==(const Duration& other) const { return this->_duration == other._duration; }

    Duration& operator-=(const Duration& other);
    Duration& operator+=(const Duration& other);

public:
    static Duration seconds(long s) { return Duration(s * 1000); }
    static Duration milliseconds(long ms) { return Duration(ms); }
    static Duration maximum();
    static Duration min(const Duration& d1, const Duration& d2) {
        return d1 < d2 ? d1 : d2;
    }

private:
    long _duration;
};
}
