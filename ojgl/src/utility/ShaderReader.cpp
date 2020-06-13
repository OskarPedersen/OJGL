#include "Log.h"
#include "ShaderReader.h"
#include "utility/Macros.h"
#include "utility/OJstd.h"
#ifdef _DEBUG
#include <fstream>
#include <sstream>
#include <streambuf>
#include <string>
#include <sys/stat.h>
#include <sys/types.h>
#endif

using namespace ojgl;

namespace {
#ifdef _DEBUG
long long modifyTime(const ojstd::string& path)
{
    struct stat fileStat;
    return stat(path.c_str(), &fileStat) == 0 ? fileStat.st_mtime : 0;
}

bool fileExists(const ojstd::string& path)
{
    struct stat fileStat;
    return stat(path.c_str(), &fileStat) == 0;
}
#endif

ojstd::string replaceIncludes(const ojstd::string& rawShader)
{
    ojstd::string includeKeyword = R""(#include ")"";
    ojstd::string modifiedShader = rawShader;

    int startPos = 0;
    int includeStart = rawShader.find(includeKeyword, startPos);
    while (includeStart != -1) {
        int nameStart = includeStart + includeKeyword.length();
        int nameEnd = nameStart + 1;
        while (rawShader[nameEnd] != '"')
            nameEnd++;

        int includeStringLength = includeKeyword.length() + nameEnd - nameStart + 2;
        ojstd::string name = rawShader.substring(nameStart, nameEnd);
        ojstd::string includeString = rawShader.substring(includeStart, includeStart + includeStringLength);

        const auto& shaderToInclude = ShaderReader::get(name);
        modifiedShader = modifiedShader.replaceFirst(includeString, shaderToInclude);
        startPos += includeStringLength;
        includeStart = rawShader.find(includeKeyword, startPos);
    }

    return modifiedShader;
}
}

void ShaderReader::preLoad(const ojstd::string& path, const ojstd::string& content)
{
    ShaderReader::_shaders[path].content = replaceIncludes(content);
}

void ShaderReader::setBasePath(const ojstd::string& basePath)
{
    ShaderReader::_basePath = basePath;
}

bool ShaderReader::modified(const ojstd::string& path)
{
#ifdef _DEBUG
    return modifyTime(ShaderReader::_basePath + path) != ShaderReader::_shaders[path].modifyTime;
#else
    OJ_UNUSED(path);
    return false;
#endif
}

const ojstd::string& ShaderReader::get(const ojstd::string& path)
{
#ifdef _DEBUG
    auto fullPath = _basePath + path;
    //_ASSERT_EXPR(fileExists(fullPath), ojstd::wstringWrapper(fullPath + " not found.").ptr);
    if (!fileExists(fullPath)) {
        return ShaderReader::_shaders[path].content;
    }
    if (modified(path)) {
        LOG_INFO("[" << path.c_str() << "]"
                     << " modified");

        std::ifstream shaderFile;
        // Enable exceptions to try and get more info about why the shader reader sometimes fails when reloading
        std::ios_base::iostate exceptionMask = shaderFile.exceptions() | std::ios::failbit;
        shaderFile.exceptions(exceptionMask);

        try {
            shaderFile.open(fullPath.c_str());
        } catch (std::ios_base::failure& e) {
            std::cerr << "ShaderReader failed: " << e.what() << '\n';
        }
        _ASSERTE(!shaderFile.fail());

        std::stringstream buffer;
        buffer << shaderFile.rdbuf();
        std::string fileContents = buffer.str();
        std::string pre = "R\"\"(";
        std::string post = ")\"\"";
        size_t start = fileContents.find(pre);
        size_t end = fileContents.rfind(post);
        std::string shader = fileContents.substr(start + pre.length(), end - start - pre.length());
        ShaderReader::_shaders[path].content = replaceIncludes(shader.c_str());
        ShaderReader::_shaders[path].modifyTime = modifyTime(fullPath);
    }
#endif
    return ShaderReader::_shaders[path].content;
}

ojstd::unordered_map<ojstd::string, ShaderContent> ShaderReader::_shaders;
ojstd::string ShaderReader::_basePath;
