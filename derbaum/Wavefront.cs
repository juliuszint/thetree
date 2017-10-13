using OpenTK;
using System;
using System.Globalization;
using System.IO;

namespace derbaum
{
    public static class Wavefront
    {
        public static ObjectVertexData Load(string fileName)
        {
            var counting = true;
            var result = new ObjectVertexData();
            var stringContent = File.ReadAllText(fileName);
            var stringLines = StringSplitWithCount(stringContent, "\n");
            var partMultiArray = new string[stringLines.Length][];
            for(int i = 0; i < partMultiArray.Length; i++) {
                partMultiArray[i] = StringSplitWithCount(stringLines[i], " ");
            }
            int vCount = 0, vtCount = 0, vnCount = 0, fCount = 0;
            int vIndex = 0, vtIndex = 0, vnIndex = 0, fIndex = 0;
            Vector3[] vertices = null;
            Vector3[] normals = null;
            Vector2[] uvs = null;
            TriangleIndexContext[] triangles = null;

            while(true) {
                foreach (var parts in partMultiArray) {
                    if (parts.Length < 0 || parts[0].StartsWith("#"))
                        continue;

                    switch (parts[0]) {
                        case "v": {
                                if (counting)
                                    vCount++;
                                else {
                                    vertices[vIndex++] = new Vector3(
                                        float.Parse(parts[1], CultureInfo.InvariantCulture),
                                        float.Parse(parts[2], CultureInfo.InvariantCulture),
                                        float.Parse(parts[3], CultureInfo.InvariantCulture));
                                }
                            } break;
                        case "vt": {
                                if(counting) 
                                    vtCount++;
                                else {
                                    uvs[vtIndex++] = new Vector2(
                                        float.Parse(parts[1], CultureInfo.InvariantCulture),
                                        1.0f - float.Parse(parts[2], CultureInfo.InvariantCulture));
                                }
                            } break;
                        case "vn": {
                                if(counting) 
                                    vnCount++;
                                else {
                                    normals[vnIndex++] = new Vector3(
                                        float.Parse(parts[1], CultureInfo.InvariantCulture),
                                        float.Parse(parts[2], CultureInfo.InvariantCulture),
                                        float.Parse(parts[3], CultureInfo.InvariantCulture));
                                }
                            } break;
                        case "f": {
                                if(counting) 
                                    fCount++;
                                else {
                                    IndexContextFromString(parts[1], ref triangles[fIndex++]);
                                    IndexContextFromString(parts[2], ref triangles[fIndex++]);
                                    IndexContextFromString(parts[3], ref triangles[fIndex++]);
                                }
                            } break;
                        default: {
                                BaumEnvironment.Log(LogLevel.Warning,
                                                    $"ignoring {parts[0]} while loading obj file");
                            } break;
                    }
                }
                if(counting) {
                    vertices = new Vector3[vCount];
                    uvs = new Vector2[vtCount];
                    normals = new Vector3[vnCount];
                    triangles = new TriangleIndexContext[fCount * 3];

                    result.Vertices = new Vector3[fCount * 3];
                    result.Normals = new Vector3[fCount * 3];
                    result.UVs = new Vector2[fCount * 3];
                    result.Indices = new int[fCount * 3];
                }
                else {
                    break;
                }
                counting = false;
            }

            for(int i = 0; i < triangles.Length; i++) {
                var triangleVertex = triangles[i];
                result.Vertices[i] = vertices[triangleVertex.VertexIndex];
                result.Normals[i] = normals[triangleVertex.NormalIndex];
                result.UVs[i] = uvs[triangleVertex.UvIndex];
                result.Indices[i] = i;
            }

            return result;
        }

        private static void IndexContextFromString(string text, ref TriangleIndexContext context)
        {
            var indicesText = text.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            context.VertexIndex = int.Parse(indicesText[0]) - 1;
            context.NormalIndex = int.Parse(indicesText[2]) - 1;
            context.UvIndex     = int.Parse(indicesText[1]) - 1;
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

        private struct TriangleIndexContext
        {
            public int VertexIndex;
            public int NormalIndex;
            public int UvIndex;
        }
    }
}
