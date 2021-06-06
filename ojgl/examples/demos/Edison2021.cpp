#include "Edison2021.h"
#include "FreeCameraController.h"
#include "music/Music.h"

using namespace ojgl;

ojstd::string Edison2021::getTitle() const
{
    return "OJ - Edison2021";
}

static const unsigned char song[] = {
#include "songs/edison2021.inc"
};

const unsigned char* Edison2021::getSong() const
{
    return song;
}

ojstd::vector<Scene> Edison2021::buildSceneGraph(const Vector2i& sceneSize) const
{

    ojstd::vector<Scene> scenes;

    {
      auto raymarch = Buffer::construct(sceneSize.x, sceneSize.y, "common/quad.vs", "edison2021/scene2.fs");

      auto edge = Buffer::construct(sceneSize.x, sceneSize.y, "common/quad.vs", "edison2021/scene2edge.fs");
      edge->setInputs(raymarch);

      auto blur = Buffer::construct(sceneSize.x, sceneSize.y, "common/quad.vs", "edison2021/scene2blur.fs");
      blur->setInputs(edge);

      auto post = Buffer::construct(sceneSize.x, sceneSize.y, "common/quad.vs", "edison2021/scene2post.fs");
      post->setInputs(raymarch, blur);

      raymarch->setUniformCallback([](float relativeSceneTime) {
        Buffer::UniformVector vector;
        vector.push_back(ojstd::make_shared<UniformMatrix4fv>("iCameraMatrix", FreeCameraController::instance().getCameraMatrix()));

        auto music = Music::instance();
        //float m = ojstd::min(music->syncChannels()[0].getTimeToNext(0).toSeconds(), music->syncChannels()[0].getTimeToNext(1).toSeconds());
        //vector.push_back(ojstd::make_shared<Uniform1f>("CHANNEL_0_TO", m));
        vector.push_back(ojstd::make_shared<Uniform1f>("CHANNEL_0_TOTAL", music->syncChannels()[0].getTotalHits()));
        vector.push_back(ojstd::make_shared<Uniform1f>("CHANNEL_0_SINCE", music->syncChannels()[0].getTimeSinceAnyLast().toSeconds()));
        vector.push_back(ojstd::make_shared<Uniform1f>("CHANNEL_0_TO", music->syncChannels()[0].getTimeToAnyNext().toSeconds()));
        vector.push_back(ojstd::make_shared<Uniform1f>("CHANNEL_0_LAST_NOTE", music->syncChannels()[0].getLastNote()));
        return vector;
      });

      scenes.emplace_back(post, Duration::seconds(999999));
    }

    {
        auto raymarch = Buffer::construct(sceneSize.x, sceneSize.y, "common/quad.vs", "edison2021/scene1.fs");

        raymarch->setUniformCallback([](float relativeSceneTime) {
            Buffer::UniformVector vector;
            vector.push_back(ojstd::make_shared<UniformMatrix4fv>("iCameraMatrix", FreeCameraController::instance().getCameraMatrix()));

            auto music = Music::instance();
            //float m = ojstd::min(music->syncChannels()[0].getTimeToNext(0).toSeconds(), music->syncChannels()[0].getTimeToNext(1).toSeconds());
            //vector.push_back(ojstd::make_shared<Uniform1f>("CHANNEL_0_TO", m));
            vector.push_back(ojstd::make_shared<Uniform1f>("CHANNEL_0_TOTAL", music->syncChannels()[0].getTotalHits()));
            vector.push_back(ojstd::make_shared<Uniform1f>("CHANNEL_0_SINCE", music->syncChannels()[0].getTimeSinceAnyLast().toSeconds()));
            return vector;
        });

        scenes.emplace_back(raymarch, Duration::seconds(999999));
    }

    return scenes;
}