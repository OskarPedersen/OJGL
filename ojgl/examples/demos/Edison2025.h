#pragma once

#include "demo/Demo.h"
#include "render/Scene.h"
#include "render/Texture.h"
#include "utility/OJstd.h"

namespace ojgl {

class Edison2025 final : public Demo {
public:
    Edison2025();
    ojstd::vector<Scene> buildSceneGraph(const Vector2i& sceneSize) const override;
    const unsigned char* getSong() const override;
    ojstd::string getTitle() const override;
    void update(const Duration& relativeSceneTime, const Duration& elapsedTime, const ojstd::string& currentScene) const override;

private:
    ojstd::shared_ptr<Texture> getText(const ojstd::string& text, const ojstd::string& font) const;
    mutable ojstd::unordered_map<ojstd::string, ojstd::shared_ptr<Texture>> _textures;
};

}
