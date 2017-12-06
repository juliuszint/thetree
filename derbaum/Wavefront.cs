using OpenTK;
using System;
using System.Globalization;
using System.IO;

namespace derbaum
{
    public static class Wavefront
    {
        const string WavefrontNormal = "vn";
        const string WavefrontVertex = "v";
        const string WavefrontUv = "vt";
        const string WavefrontTriangle = "f";

        private struct WavefrontFace
        {
            public int VertexIndex1;
            public int VertexIndex2;
            public int VertexIndex3;

            public int NormalIndex1;
            public int NormalIndex2;
            public int NormalIndex3;

            public int UvIndex1;
            public int UvIndex2;
            public int UvIndex3;

            public Vector3 Tangent;
            public Vector3 Bitangent;
        }

        private struct WavefrontFileData
        {
            public Vector3[] Vertices;
            public Vector3[] Normals;
            public Vector2[] Uvs;
            public WavefrontFace[] Triangles;
        }

        public static ObjectVertexData Load(string fileName)
        {
            var stringContent = File.ReadAllText(fileName);
            var textContent = SplitStringContent(stringContent);
            var inMemoryWavefront = AllocateMemoryForFile(textContent);
            ParseFile(inMemoryWavefront, textContent);
            CalculateTangentsAndBiTangents(inMemoryWavefront);
            var result = CreateVertexDataObject(inMemoryWavefront);
            return result;
        }

        private static void CalculateTangentsAndBiTangents(WavefrontFileData data)
        {
            for(int i = 0; i < data.Triangles.Length; i++) {
                var triangleInfo = data.Triangles[i];
                var v1 = data.Vertices[triangleInfo.VertexIndex1];
                var v2 = data.Vertices[triangleInfo.VertexIndex2];
                var v3 = data.Vertices[triangleInfo.VertexIndex3];
                var uv1 = data.Uvs[triangleInfo.UvIndex1];
                var uv2 = data.Uvs[triangleInfo.UvIndex2];
                var uv3 = data.Uvs[triangleInfo.UvIndex3];
                var n1 = data.Normals[triangleInfo.NormalIndex1];

                var edge1 = v2 - v1;
                var edge2 = v3 - v1;
                var deltaUv1 = uv2 - uv1;
                var deltaUv2 = uv3 - uv1;
                float f;
                if (Math.Abs(deltaUv1.X * deltaUv2.Y - deltaUv2.X * deltaUv1.Y) < 0.0001f) {
                    f = 1.0f;
                }
                else {
                    f = 1.0f / (deltaUv1.X * deltaUv2.Y - deltaUv2.X * deltaUv1.Y);
                }
                var tangent = new Vector3(f * (deltaUv2.Y * edge1.X - deltaUv1.Y * edge2.X),
                                          f * (deltaUv2.Y * edge1.Y - deltaUv1.Y * edge2.Y),
                                          f * (deltaUv2.Y * edge1.Z - deltaUv1.Y * edge2.Z));
                tangent.Normalize();

                var biTangent = new Vector3(f * (-deltaUv2.X * edge1.X + deltaUv1.X * edge2.X),
                                           f * (-deltaUv2.X * edge1.Y + deltaUv1.X * edge2.Y),
                                           f * (-deltaUv2.X * edge1.Z + deltaUv1.X * edge2.Z));
                biTangent.Normalize();
                if (Vector3.Dot(Vector3.Cross(n1, tangent), biTangent) < 0.0f) {
                    tangent = tangent * -1.0f;
                }
                triangleInfo.Tangent = tangent;
                triangleInfo.Bitangent = biTangent;
            }
        }

        private static ObjectVertexData CreateVertexDataObject(WavefrontFileData data)
        {
            var result = new ObjectVertexData();
            result.Vertices = new Vector3[data.Triangles.Length * 3];
            result.Normals = new Vector3[data.Triangles.Length * 3];
            result.UVs = new Vector2[data.Triangles.Length * 3];
            result.Tangents = new Vector3[data.Triangles.Length * 3];
            result.BiTangents = new Vector3[data.Triangles.Length * 3];
            result.Indices = new int[data.Triangles.Length * 3];

            for(int i = 0; i < data.Triangles.Length; i++) {
                var triangleInfo = data.Triangles[i];
                var index = i * 3;
                result.Vertices[index + 0] = data.Vertices[triangleInfo.VertexIndex1];
                result.Vertices[index + 1] = data.Vertices[triangleInfo.VertexIndex2];
                result.Vertices[index + 2] = data.Vertices[triangleInfo.VertexIndex3];

                result.Normals[index + 0] = data.Vertices[triangleInfo.NormalIndex1];
                result.Normals[index + 1] = data.Vertices[triangleInfo.NormalIndex2];
                result.Normals[index + 2] = data.Vertices[triangleInfo.NormalIndex3];

                result.UVs[index + 0] = data.Uvs[triangleInfo.UvIndex1];
                result.UVs[index + 1] = data.Uvs[triangleInfo.UvIndex2];
                result.UVs[index + 2] = data.Uvs[triangleInfo.UvIndex3];

                result.Tangents[index + 0] = triangleInfo.Tangent;
                result.Tangents[index + 1] = triangleInfo.Tangent;
                result.Tangents[index + 2] = triangleInfo.Tangent;

                result.BiTangents[index + 0] = triangleInfo.Bitangent;
                result.BiTangents[index + 1] = triangleInfo.Bitangent;
                result.BiTangents[index + 2] = triangleInfo.Bitangent;

                result.Indices[index + 0] = index + 0;
                result.Indices[index + 1] = index + 1;
                result.Indices[index + 2] = index + 2;
            }

            return result;
        }

        private static WavefrontFileData AllocateMemoryForFile(string[][] lines)
        {
            var result = new WavefrontFileData();
            int vertexCount = 0;
            int normalCount = 0;
            int triangleCount = 0;
            int uvCount = 0;
            foreach(string[] content in lines) {
                if (content.Length <= 0)
                    continue;

                switch(content[0]) {
                    case WavefrontVertex:
                        vertexCount++;
                        break;
                    case WavefrontNormal:
                        normalCount++;
                        break;
                    case WavefrontUv:
                        uvCount++;
                        break;
                    case WavefrontTriangle:
                        triangleCount++;
                        break;
                }
            }
            result.Vertices = new Vector3[vertexCount];
            result.Normals = new Vector3[normalCount];
            result.Uvs = new Vector2[uvCount];
            result.Triangles = new WavefrontFace[triangleCount];
            return result;
        }

        private static void ParseFile(WavefrontFileData data, string[][] textcontent)
        {
            int vertexIndex = 0;
            int uvIndex = 0;
            int normalIndex = 0;
            int triangleIndex = 0;
            foreach(string[] content in textcontent) {
                if (content.Length < 0 || content[0].StartsWith("#")) {
                    continue;
                }

                switch(content[0]) {
                    case WavefrontVertex:
                        data.Vertices[vertexIndex++] = new Vector3(
                            float.Parse(content[1], CultureInfo.InvariantCulture),
                            float.Parse(content[2], CultureInfo.InvariantCulture),
                            float.Parse(content[3], CultureInfo.InvariantCulture));
                        break;
                    case WavefrontNormal:
                        data.Normals[normalIndex++] = new Vector3(
                            float.Parse(content[1], CultureInfo.InvariantCulture),
                            float.Parse(content[2], CultureInfo.InvariantCulture),
                            float.Parse(content[3], CultureInfo.InvariantCulture));
                        break;
                    case WavefrontUv:
                        data.Uvs[uvIndex++] = new Vector2(
                            float.Parse(content[1], CultureInfo.InvariantCulture),
                            1.0f - float.Parse(content[2], CultureInfo.InvariantCulture));
                        break;
                    case WavefrontTriangle:
                        var indicesText = content[1].Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
                        data.Triangles[triangleIndex].VertexIndex1 = int.Parse(indicesText[0]) - 1;
                        data.Triangles[triangleIndex].NormalIndex1 = int.Parse(indicesText[2]) - 1;
                        data.Triangles[triangleIndex].UvIndex1     = int.Parse(indicesText[1]) - 1;

                        indicesText = content[2].Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
                        data.Triangles[triangleIndex].VertexIndex2 = int.Parse(indicesText[0]) - 1;
                        data.Triangles[triangleIndex].NormalIndex2 = int.Parse(indicesText[2]) - 1;
                        data.Triangles[triangleIndex].UvIndex2     = int.Parse(indicesText[1]) - 1;

                        indicesText = content[3].Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
                        data.Triangles[triangleIndex].VertexIndex3 = int.Parse(indicesText[0]) - 1;
                        data.Triangles[triangleIndex].NormalIndex3 = int.Parse(indicesText[2]) - 1;
                        data.Triangles[triangleIndex].UvIndex3     = int.Parse(indicesText[1]) - 1;

                        triangleIndex++;
                        break;
                }
            }
        }

        private static string[][] SplitStringContent(string fileContent)
        {
            var stringLines = StringSplitWithCount(fileContent, "\n");
            var partMultiArray = new string[stringLines.Length][];
            for(int i = 0; i < partMultiArray.Length; i++) {
                partMultiArray[i] = StringSplitWithCount(stringLines[i], " ");
            }
            return partMultiArray;
        }

        private static string[] StringSplitWithCount(string content, string splitAt)
        {
            var _counting = true;
            var _partCount = 0;
            var _matchIndex = 0;
            var _partStartIndex = 0;
            var _resultPartIndex = 0;
            var _result = new string[0];

            while(true) {
                for (int i = 0; i < content.Length; i++) {
                    if (content[i] == splitAt[_matchIndex]) {
                        _matchIndex++;
                        if (_matchIndex == splitAt.Length) {
                            var _lineEndIndex = i - splitAt.Length + 1;
                            var _charCount = _lineEndIndex - _partStartIndex;
                            if (_charCount > 0) {
                                if (_counting) {
                                    _partCount++;
                                }
                                else {
                                    _result[_resultPartIndex++] = content.Substring(_partStartIndex,
                                                                                    _charCount);
                                }
                            }
                            _matchIndex = 0;
                            _partStartIndex = i + 1;
                        }
                    }
                }
                if(_counting) {
                    if(_partStartIndex < content.Length) {
                        _partCount++;
                    }
                    _result = new string[_partCount];
                }
                else {
                    if(_partStartIndex != content.Length) {
                        _result[_resultPartIndex] = content.Substring(_partStartIndex);
                    }
                    break;
                }
                _partStartIndex = 0;
                _counting = false;
            }
            return _result;
        }
    }
}
