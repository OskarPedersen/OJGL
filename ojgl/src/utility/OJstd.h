#pragma once
#include <utility>

namespace ojstd {

float sin(float angle);
float cos(float angle);
float tan(float angle);
int ftoi(float value);

template <typename T>
T max(const T& a, const T& b)
{
    return a >= b ? a : b;
}

template <typename T>
T min(const T& a, const T& b)
{
    return a <= b ? a : b;
}

template <class InputIt, class T>
T accumulate(InputIt first, InputIt last, T init)
{
    for (; first != last; ++first) {
        init = std::move(init) + *first;
    }
    return init;
}

template <class ForwardIt, class UnaryPredicate>
ForwardIt find_if(ForwardIt first, ForwardIt last, UnaryPredicate p)
{
    for (auto it = first; it != last; it++) {
        if (p(*it)) {
            return it;
        }
    }
    return last;
}

template <typename T>
class shared_ptr {
public:
    shared_ptr(T* ptr)
        : _ptr(ptr)
    {
        _count = new int;
        *_count = 1;
    }
    shared_ptr()
        : shared_ptr(nullptr)
    {
    }
    shared_ptr(const shared_ptr<T>& other)
    {
        this->_ptr = other.get();
        this->_count = other._count;
        (*this->_count)++;
    }
    ~shared_ptr()
    {
        (*_count)--;
        try_free();
    }
    shared_ptr& operator=(const shared_ptr& other)
    {
        if (*this != other) {
            (*_count)--;
            try_free();
            this->_ptr = other.get();
            this->_count = other._count;
            (*this->_count)++;
        }
        return *this;
    }
    template <typename B>
    shared_ptr<T>& operator=(const shared_ptr<B>& other)
    {
        if (*this != other) {
            (*_count)--;
            try_free();
            this->_ptr = other.get();
            this->_count = other._count;
            (*this->_count)++;
        }
        return *this;
    }
    T* operator->() const { return _ptr; }
    T& operator*() const noexcept { return *_ptr; }
    T* get() const { return _ptr; }
    bool operator==(const shared_ptr<T>& other) const { return _ptr == other._ptr && _count == other._count; }
    bool operator!=(const shared_ptr<T>& other) const { return !(*this == other); }

    template <typename B>
    bool operator==(const shared_ptr<B>& other) const { return _ptr == other._ptr && _count == other._count; }
    template <typename B>
    bool operator!=(const shared_ptr<B>& other) const { return !(*this == other); }

    bool operator==(std::nullptr_t other) const { return _ptr == other; }
    bool operator!=(std::nullptr_t other) const { return _ptr != other; }

    int* _count = nullptr; // @todo make private and thread-safe.
    T* _ptr = nullptr;

private:
    void try_free()
    {
        _ASSERTE(*_count >= 0);
        if (*_count == 0) {
            if (_count)
                delete _count;
            if (_ptr)
                delete _ptr;
        }
    }
};

template <typename T, typename... Args>
shared_ptr<T> make_shared(Args... args)
{
    return shared_ptr<T>(new T(args...));
}

template <typename T>
class vector {
public:
    vector()
    {
        this->values = (T*)calloc(this->capacity, sizeof(T));
    }
    vector(const vector<T>& other)
    {
        this->capacity = max(MIN_CAPACITY, other.size());
        this->values = (T*)calloc(this->capacity, sizeof(T));
        this->length = other.size();
        for (int i = 0; i < other.size(); i++) {
            new (values + i) T(other[i]);
        }
    }
    vector(const std::initializer_list<T>& other)
    {
        this->capacity = max(MIN_CAPACITY, static_cast<int>(other.size()));
        this->values = (T*)calloc(this->capacity, sizeof(T));
        this->length = other.size();
        int i = 0;
        for (auto& x : other) {
            new (values + i) T(x);
            i++;
        }
    }
    ~vector()
    {
        if (this->values) {
            for (int i = 0; i < this->length; i++)
                this->values[i].~T();
            free(this->values);
        }
    }

public:
    vector<T>& operator=(const vector<T>& other)
    {
        this->clear();
        if (this->values)
            free(this->values);
        this->capacity = max(MIN_CAPACITY, other.size());
        this->values = (T*)calloc(this->capacity, sizeof(T));
        this->length = other.size();
        for (int i = 0; i < other.size(); i++) {
            new (values + i) T(other[i]);
        }
        return *this;
    }
    T& operator[](int index) const
    {
        _ASSERTE(index >= 0);
        _ASSERTE(index < length);
        return values[index];
    }

public:
    int size() const { return length; }
    T* begin() const { return values; }
    T* end() const { return values + length; }
    const T* cbegin() const { return values; }
    const T* cend() const { return values + length; }
    bool empty() const { return length == 0; }

    template <class... Args>
    void emplace_back(Args&&... args)
    {
        if (length == capacity)
            this->resize(max(MIN_CAPACITY, this->capacity * 2));
        new (values + length) T(std::forward<Args>(args)...);
        length++;
    }
    void push_back(const T& val)
    {
        if (length == capacity)
            this->resize(max(MIN_CAPACITY, this->capacity * 2));
        new (values + length) T(val);
        length++;
    }
    T* erase(T* it)
    {
        if (it >= this->begin() && it < this->end()) {
            it->~T();
            memmove(it, it + 1, sizeof(T) * (this->end() - it - 1));
            this->length--;
            if ((it + 1) == this->end())
                return this->end();
            else
                return it;
        } else
            return this->end();
    }
    void clear()
    {
        for (int i = 0; i < this->length; i++)
            this->values[i].~T();
        length = 0;
    }
    void resize(int size)
    {
        T* newBuffer = (T*)calloc(size, sizeof(T));
        _ASSERTE(size >= this->length);
        memcpy(newBuffer, this->values, sizeof(T) * this->length);
        if (this->values)
            free(this->values);
        this->values = newBuffer;
        this->capacity = size;
    }

private:
    static int constexpr MIN_CAPACITY = 10;
    int length = 0;
    int capacity = MIN_CAPACITY;
    T* values;
};

template <typename First, typename Second>
struct Pair {
    Pair(const First& first, const Second& second)
        : first(first)
        , second(second)
    {
    }
    bool operator==(const Pair<First, Second>& other) const { return first == other.first && second == other.second; }

    First first;
    Second second;
};

template <typename K, typename V>
class unordered_map {
public:
    unordered_map()
    {
    }

    V& operator[](const K& key)
    {
        auto it = find_if(this->keyValuePairs.begin(), this->keyValuePairs.end(), [&key](const auto& kvp) { return kvp.first == key; });
        if (it == this->keyValuePairs.end()) {
            this->keyValuePairs.emplace_back(key, V());
            return this->keyValuePairs[this->keyValuePairs.size() - 1].second;
        } else {
            return it->second;
        }
    }
    Pair<K, V>* find(const K& key)
    {
        return find_if(this->keyValuePairs.begin(), this->keyValuePairs.end(), [&key](const auto& kvp) { return kvp.first == key; });
    }
    Pair<K, V>* begin() { return keyValuePairs.begin(); }
    Pair<K, V>* end() { return keyValuePairs.end(); }

private:
    vector<Pair<K, V>> keyValuePairs;
};

template <typename T>
class unordered_set {
public:
    void insert(const T& val)
    {
        auto it = find_if(this->begin(), this->end(), [&val](const auto& v) { return v == val; });
        if (it == this->end())
            this->values.push_back(val);
    }
    T* find(const T& val)
    {
        return find_if(this->begin(), this->end(), [&val](const auto& v) { return v == val; });
    }
    size_t erase(const T& val)
    {
        int nprev = this->values.size();
        auto it = find_if(this->begin(), this->end(), [&val](const auto& v) { return v == val; });
        this->erase(it);
        int npost = this->values.size();
        return nprev - npost;
    }
    T* begin() { return values.begin(); }
    T* end() { return values.end(); }
    const T* cbegin() const { return values.cbegin(); }
    const T* cend() const { return values.cend(); }
    bool empty() { return this->values.size() == 0; }
    T* erase(T* it) { return this->values.erase(it); }

private:
    vector<T> values;
};

class string {
public:
    string(const char* str);
    string(const string& str);
    string();
    ~string();
    bool operator==(const string& other) const;
    string& operator=(const string& other);
    string operator+(const string& other);
    const char* c_str() const;
    int length() const;
    void append(const string& other);

private:
    char* content = nullptr;
    int len = 0;
};

string to_string(size_t i);

class mutex {
public:
    mutex();
    mutex(const mutex& other) = delete;
    mutex& operator=(const mutex& other) = delete;
    ~mutex();
    void lock();
    void unlock();

private:
    class details;
    shared_ptr<details> _priv; // @todo make ojstd::unique_ptr.
};

template <typename T>
class lock_guard {
public:
    lock_guard(T& v)
        : _v(v)
    {
        _v.lock();
    }
    ~lock_guard()
    {
        _v.unlock();
    }

private:
    T& _v;
};

void sleep(int milliseconds);

struct Vec3 {
    Vec3();
    Vec3(float x, float y, float z);

    Vec3 operator*(float a);
    void operator+=(Vec3 v);
    Vec3 operator-(Vec3 v);

    const Vec3& Vec3::normalize();
    float lenSq();

    float x, y, z;
};

// https://www.gamedev.net/forums/topic/671079-fast-sqrt-for-64bit/
double inline __fastcall sqrt_asm(double n);

} //end namespace ojstd
