using System;
using System.Collections.Generic;
using System.Numerics;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using OpenTK.Input;
namespace OpenGLApp
{
    class Program
    {
        private static Matrix4 projectionMatrix;
        private static Matrix4 viewMatrix;
        private static Vector3 cameraPosition = new Vector3(0, 1, 5);
        private static Vector3 cameraTarget = Vector3.Zero;
        private static Vector3 cameraUp = Vector3.UnitY;
        private static float cameraSpeed = 0.5f;
        private static List<Light> lights = new List<Light>();
        private static Sphere[] spheres = {
new Sphere(new Vector3(0, 0, 0), 1, new Vector3(1, 0, 0)),
new Sphere(new Vector3(1, 0, -3), 1, new Vector3(0, 1, 0)),
new Sphere(new Vector3(0, 0, -5), 1, new Vector3(0, 0, 1))
};
        static void Main(string[] args)
        {
            using (GameWindow window = new GameWindow(1000, 800,
            GraphicsMode.Default, "OpenGL Example"))
            {
                window.Load += (sender, e) =>
                {
                    // Настройки инициализации OpenGL
                    GL.ClearColor(0.1f, 0.2f, 0.3f, 0.0f);
                    GL.Enable(EnableCap.DepthTest);
                    window.VSync = VSyncMode.On;
                    // Установка матрицы проекции
                    float aspectRatio = window.Width /
                    (float)window.Height;
                    float fieldOfView =
                    MathHelper.DegreesToRadians(60f);
                    float nearPlane = 0.1f;
                    float farPlane = 100f;
                    projectionMatrix =
                    Matrix4.CreatePerspectiveFieldOfView(fieldOfView, aspectRatio, nearPlane,
                    farPlane);
                    // Инициализация источников света
                    lights.Add(new Light
                    {
                        Type = LightType.Ambient,
                        Intensity = 0.4f
                    });
                    lights.Add(new Light
                    {
                        Type =
                    LightType.Directional,
                        Intensity = 0.8f,
                        Direction = new Vector3(1, 2,
                    10).Normalized()
                    });
                    lights.Add(new Light
                    {
                        Type = LightType.Point,
                        Intensity = 0.8f,
                        Position = new Vector3(2, 5, 0)
                    });
                };
                window.Resize += (sender, e) =>
                {
                    // Устанавливаем область вывода графики
                    GL.Viewport(0, 0, window.Width, window.Height);
                    projectionMatrix =
                    Matrix4.CreatePerspectiveFieldOfView(MathHelper.DegreesToRadians(60f),
                    window.Width / (float)window.Height, 0.1f, 100f);
                };
                window.UpdateFrame += (sender, e) =>
                {
                    var direction = (cameraTarget -
                    cameraPosition).Normalized();
                    var right = Vector3.Cross(direction,
                    cameraUp).Normalized();
                    // Обработка ввода для управления камерой
                    if (Keyboard.GetState().IsKeyDown(Key.W))
                        cameraPosition += direction * cameraSpeed;
                    if (Keyboard.GetState().IsKeyDown(Key.S))
                        cameraPosition -= direction * cameraSpeed;
                    if (Keyboard.GetState().IsKeyDown(Key.A))
                        cameraPosition -= right * cameraSpeed;
                    if (Keyboard.GetState().IsKeyDown(Key.D))
                        cameraPosition += right * cameraSpeed;
                    viewMatrix = Matrix4.LookAt(cameraPosition,
                    cameraTarget, cameraUp);
                };
                window.RenderFrame += (sender, e) =>
                {
                    // Очистка буфера цвета и глубины
                    GL.Clear(ClearBufferMask.ColorBufferBit |
                    ClearBufferMask.DepthBufferBit);
                    // Устанавливаем матрицы проекции и вида
                    RayTraceScene(window.Width, window.Height);
                    // Меняем буферы местами
                    window.SwapBuffers();
                };
                window.Run(60.0);
            }
        }
        private static void RayTraceScene(int width, int height)
        {
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    // Преобразуем координаты пикселя в нормализованные координаты устройства(NDC)
                float ndcX = (x / (float)width) * 2.0f - 1.0f;
                    float ndcY = 1.0f - (y / (float)height) * 2.0f;
                    // Преобразуем NDC в координаты мира
                    Vector4 rayClip = new Vector4(ndcX, ndcY, -1.0f,
                    1.0f);
                Vector4 rayEye = rayClip *
                projectionMatrix.Inverted();
                    rayEye.Z = -1.0f;
                    rayEye.W = 0.0f;
                    Vector3 rayWorld = (new Vector4(rayEye.Xyz, 0.0f)
                    * viewMatrix.Inverted()).Xyz.Normalized();
                    // Выпускаем луч из камеры
                    Vector3 color = TraceRay(cameraPosition,
                    rayWorld);
                    // Устанавливаем цвет пикселя
                    GL.Color3(color);
                    GL.Begin(PrimitiveType.Points);
                    GL.Vertex2(ndcX, ndcY);
                    GL.End();
                }
            }
        }
        private static Vector3 TraceRay(Vector3 origin, Vector3 direction)
        {
            Vector3 color = new Vector3(0.0f, 0.0f, 0.0f);
            float closestT = float.MaxValue;
            Plane plane = new Plane(new Vector3(0, -1, 0), Vector3.UnitY,
            new Vector3(0.8f, 0.8f, 0.8f));
            Vector3 intersectionPoint = Vector3.Zero;
            Sphere closestSphere = null;
            // Пересечение с плоскостью
            float tPlane;
            if (plane.Intersect(origin, direction, out tPlane))
            {
                if (tPlane < closestT)
                {
                    closestT = tPlane;
                    intersectionPoint = origin + tPlane * direction;
                    Vector3 normal = plane.Normal;
                    color = ComputeLighting(intersectionPoint, normal,
                    plane.Color, null);
                }
            }
            // Пересечение со сферой
            foreach (var sphere in spheres)
            {
                float tSphere;
                if (sphere.Intersect(origin, direction, out tSphere))
                {
                    if (tSphere < closestT)
                    {
                        closestT = tSphere;
                        intersectionPoint = origin + direction *
                        tSphere;
                        Vector3 normal = (intersectionPoint -
                        sphere.Center).Normalized();
                        closestSphere = sphere;
                        color = ComputeLighting(intersectionPoint,
                        normal, sphere.Color, sphere);
                    }
                }
            }
        if (closestT == float.MaxValue)
            {
                // Луч не пересекает объекты, возвращаем черный цвет
                return Vector3.Zero;
            }
            // Луч пересекает объект, возвращаем цвет в точке пересечения
            return color;
        }
        private static bool IsInShadow(Vector3 point, Vector3 lightDir,
        float maxT, Sphere currentSphere)
        {
            Vector3 shadowOrig = point + lightDir * 0.001f;
            Plane plane = new Plane(new Vector3(0, -1, 0), Vector3.UnitY,
            new Vector3(0.8f, 0.8f, 0.8f));
            // Проверка пересечений с плоскостью
            float tPlane;
            if (plane.Intersect(shadowOrig, lightDir, out tPlane))
            {
                if (tPlane < maxT)
                {
                    return true;
                }
            }
            // Проверка пересечений со сферами
            foreach (var sphere in spheres)
            {
                if (sphere == currentSphere)
                    continue;
                float tSphere;
                if (sphere.Intersect(shadowOrig, lightDir, out tSphere))
                {
                    if (tSphere < maxT)
                    {
                        return true;
                    }
                }
            }
            return false;
        }
        private static Vector3 ComputeLighting(Vector3 point, Vector3
        normal, Vector3 objectColor, Sphere currentSphere)
        {
            float intensity = 0.0f;
            foreach (var light in lights)
            {
                if (light.Type == LightType.Ambient)
                {
                    intensity += light.Intensity;
                }
                else
                {
                    Vector3 lightDir;
                    float maxT;
                    if (light.Type == LightType.Point)
                {
                        lightDir = (light.Position -
                        point).Normalized();
                        maxT = (light.Position - point).Length;
                    }
else
                    {
                        lightDir = light.Direction;
                        maxT = float.MaxValue;
                    }
                    // Проверка теней
                    if (IsInShadow(point, lightDir, maxT,
                    currentSphere))
                    {
                        continue; // Точка находится в тени
                    }
                    float nDotL = Vector3.Dot(normal, lightDir);
                    if (nDotL > 0)
                    {
                        intensity += light.Intensity * nDotL /
                        (normal.Length * lightDir.Length);
                    }
                }
            }
            return objectColor * intensity;
        }
        enum LightType { Ambient, Point, Directional }
        class Light
        {
            public LightType Type;
            public float Intensity;
            public Vector3 Position;
            public Vector3 Direction;
        }
        class Plane
        {
            public Vector3 Point;
            public Vector3 Normal;
            public Vector3 Color;
            public Plane(Vector3 point, Vector3 normal, Vector3 color)
            {
                Point = point;
                Normal = normal.Normalized();
                Color = color;
            }
            public bool Intersect(Vector3 origin, Vector3 direction, out
            float t)
            {
                t = 0;
                float denom = Vector3.Dot(Normal, direction);
                if (Math.Abs(denom) > 1e-6)
                {
                    t = Vector3.Dot(Point - origin, Normal) / denom;
                    return t >= 0;
                }
                return false;
            }
        }
        class Sphere
        {
            public Vector3 Center;
            public float Radius;
            public Vector3 Color;
            public Sphere(Vector3 center, float radius, Vector3 color)
            {
                Center = center;
                Radius = radius;
                Color = color;
            }
            public bool Intersect(Vector3 origin, Vector3 direction, out
            float t)
            {
                t = 0;
                Vector3 L = Center - origin;
                float tca = Vector3.Dot(L, direction);
                if (tca < 0) return false;
                float d2 = Vector3.Dot(L, L) - tca * tca;
                if (d2 > Radius * Radius) return false;
                float thc = (float)Math.Sqrt(Radius * Radius - d2);
                t = tca - thc;
                float t1 = tca + thc;
                if (t < 0) t = t1;
                if (t < 0) return false;
                return true;
            }
        }
    }
}