#include "SolarSystem.h"

Planet::Planet(float mass, Vec3 pos, Vec3 vel)
    : mass(mass)
    , pos(pos)
    , vel(vel)
{
}

SolarSystem::SolarSystem()
{
    // Earth
    planets.emplace_back(5.9736E+24,
        Vec3(-2.697729070933774E+07, 1.446110081509904E+08, 3.296076411029696E+04),
        Vec3(-2.977162072836500E+01, -5.642530058471864E+00, -9.703736651274220E-04));

    // Sun
    planets.emplace_back(1.9891E+30,
        Vec3(3.708069410204890E+05, 7.347520252301828E+04, 3.887218910771462E+03),
        Vec3(-4.606127026089634E-03, 7.772902526292513E-03, 6.836544921898788E-05));

    // Mercury
    planets.emplace_back(3.302E+23,
        Vec3(2.944795931327176E+07, 3.673284879527312E+07, 3.187668663728386E+05),
        Vec3(-4.783278118437298E+01, 3.231351038493616E+01, 7.036254471448057E+00));

    // Venus
    planets.emplace_back(48.685E+23,
        Vec3(5.612647151655690E+07, -9.340567851874861E+07, -4.472777981490206E+06),
        Vec3(2.983438104670340E+01, 1.782622362853692E+01, -1.485258796109859E+00));

    // Mars
    planets.emplace_back(6.4185E+23,
        Vec3(-1.487549755030171E+08, -1.774231244334544E+08, -1.512110244126618E+04),
        Vec3(1.947148471410864E+01, -1.351350153821790E+01, -7.652422983913167E-01));

    // Jupiter
    planets.emplace_back(1898.13E+24,
        Vec3(-6.913702017940772E+08, -4.284012308018234E+08, 1.726313004182798E+07),
        Vec3(6.728865193791407E+00, -1.050116335198787E+01, -1.081880249745968E-01));

    // Saturnus
    planets.emplace_back(5.68319E+26,
        Vec3(1.106094425383184E+09, 8.238407435712537E+08, -5.838019485357153E+07),
        Vec3(-6.298469201435907E+00, 7.722993150215114E+00, 1.137317446842210E-01));

    // Uranus
    planets.emplace_back(86.8103E+24,
        Vec3(1.308726219550735E+09, -2.639106654294327E+09, -2.683702794167185E+07),
        Vec3(6.049730968527591E+00, 2.709301878675839E+00, -6.841965498253189E-02));

    // Neptunus
    planets.emplace_back(102.41E+24,
        Vec3(-1.631896332892333E+09, 4.174688369977927E+09, -4.836543444482803E+07),
        Vec3(-5.092803136099079E+00, -1.943289919580870E+00, 1.575127744812126E-01));

    // Moon
    planets.emplace_back(734.9E+20,
        Vec3(-2.687887244321885E+07, 1.442239856855676E+08, 3.694458400532603E+03),
        Vec3(-2.882461495739722E+01, -5.368474982969579E+00, -4.908562379039338E-02));
}

SolarSystem::~SolarSystem()
{
}

vector<float> SolarSystem::getValues()
{
    float scale = 10.0 / 2E+09;
    vector<float> values;
    for (int i = 0; i < planets.size(); i++) {
        values.emplace_back(planets[i].pos.x * scale);
        values.emplace_back(planets[i].pos.y * scale);
        values.emplace_back(planets[i].pos.z * scale);
    }
    return values;
    /*  static float time = 0.0;
    time += 1.0 / 60.0;
    ojstd::vector<float> pos;
    for (int i = 0; i < 9; i++) {
        float t = time;
        pos.emplace_back(ojstd::sin(t) * i);
        pos.emplace_back(0.0);
        pos.emplace_back(ojstd::cos(t) * i);
    }
    return pos;*/
}

void SolarSystem::tick()
{
    const float G = 6.673e-20; //G �r upph�jt i -20 pga att den ska r�knas i km och inte i m
    float dt = 10.0; // *24.0;
    vector<Vec3> forces;
    for (int i = 0; i < planets.size(); i++) {
        for (int j = 0; j < planets.size(); j++) {
            if (i != j) {
                float r2 = (planets[i].pos, planets[j].pos).lenSq();
                float a = G * planets[j].mass / r2;
                // v = v0 + a *dt
                Vec3 dir = (planets[j].pos - planets[i].pos).normalize();
                planets[i].vel += dir * a * dt;
                // s = s0 + v * dt
                planets[i].pos += planets[i].vel * dt;
            }
        }
    }
}