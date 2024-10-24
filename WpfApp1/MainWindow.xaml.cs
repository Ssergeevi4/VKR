using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using HelixToolkit.Wpf;
using System.Linq;
namespace WpfApp2
{
    #region MainWindow
    public partial class MainWindow : Window
    {
        private PerspectiveCamera camera;
        private Point3D cameraPosition;
        private Vector3D cameraLookDirection;
        private Vector3D cameraUpDirection;
        private double cameraSpeed = 0.1;
        private double rotationSpeed = 1.0;
        private System.Windows.Point previousMousePosition;
        private bool isDragging = false;
        private ModelVisual3D selectedModel;
        private Point3D initialModelPosition;
        private bool movingDirectionalLight = false;
        private bool movingPointLight = false;
        private bool movingSpotLight = false;
        private Vector3D _directionalLightDirection;
        private Point3D _pointLightPosition;
        private Point3D _spotLightPosition;
        private ArrowVisual3D xAxis;
        private ArrowVisual3D yAxis;
        private ArrowVisual3D zAxis;
        public MainWindow()
        {
            InitializeComponent();
            InitializeViewport();
            Ligthing();
            InitializeCamera();
            MouseWheel += Wheel;
            MouseLeftButtonDown += MLBDown;
            MouseLeftButtonUp += MLBUp;
            MouseRightButtonDown += MRBDown;
            MouseRightButtonUp += MRBUp;
            MouseMove += MMove;
            MouseDoubleClick += MDoubleClick;
            KeyDown += Keys;
            KeyDown += KeyLights;
        }
//поля ввода координат источников света
private void UpdatePositions(object sender, RoutedEventArgs e)
        {
            // Обновление направления DirectionalLight
            if (TryParseTextBox(DirectionalLightDirectionX, out double dX)
            &&
            TryParseTextBox(DirectionalLightDirectionY, out double
            dY) &&
            TryParseTextBox(DirectionalLightDirectionZ, out double
            dZ))
            {
                DirectionalLight.Direction = new Vector3D(dX, dY, dZ);
            }
            // Обновление позиции PointLight
            if (TryParseTextBox(PointLightPositionX, out double pX) &&
            TryParseTextBox(PointLightPositionY, out double pY) &&
            TryParseTextBox(PointLightPositionZ, out double pZ))
            {
                PointLight.Position = new Point3D(pX, pY, pZ);
            }
            // Обновление позиции SpotLight
            if (TryParseTextBox(SpotLightPositionX, out double sX) &&
            TryParseTextBox(SpotLightPositionY, out double sY) &&
            TryParseTextBox(SpotLightPositionZ, out double sZ))
            {
                SpotLight.Position = new Point3D(sX, sY, sZ);
            }
        }
        //обработска поля ввода
        private bool TryParseTextBox(TextBox textBox, out double result)
        {
            if (textBox.Text.Contains('.'))
            {
                textBox.Text = textBox.Text.Replace('.', ',');
            }
            if (string.IsNullOrWhiteSpace(textBox.Text))
            {
                result = 1;
                textBox.Background = System.Windows.Media.Brushes.White;
                // Устанавливаем координаты xyz = 1 1 1
                return true;
            }
            if (double.TryParse(textBox.Text, out result))
            {
                textBox.Background = System.Windows.Media.Brushes.White;
                // Устанавливаем белый фон при успешном вводе
                return true;
            }
            else
            {
                textBox.Background =
                System.Windows.Media.Brushes.LightPink; // Устанавливаем светло-розовый фон при ошибке
            return false;
            }
        }
// кнопки переключения света
private void KeyLights(object sender, KeyEventArgs e)
        {
            // Переключение режимов перемещения источников света
            switch (e.Key)
            {
                case Key.F1:
                    movingDirectionalLight = !movingDirectionalLight;
                    movingPointLight = false;
                    movingSpotLight = false;
                    break;
                case Key.F2:
                    movingPointLight = !movingPointLight;
                    movingDirectionalLight = false;
                    movingSpotLight = false;
                    break;
                case Key.F3:
                    movingSpotLight = !movingSpotLight;
                    movingDirectionalLight = false;
                    movingPointLight = false;
                    break;
                case Key.Escape:
                    movingSpotLight = false;
                    movingDirectionalLight = false;
                    movingPointLight = false;
                    break;
            }
        }
        private void MDoubleClick(object sender, MouseButtonEventArgs e)
        {
            System.Windows.Point mousePos = e.GetPosition(viewport);
            PointHitTestParameters hitParams = new
            PointHitTestParameters(mousePos);
            VisualTreeHelper.HitTest(viewport, null,
            HitTestResultCallback, hitParams);
        }
        private HitTestResultBehavior HitTestResultCallback(HitTestResult
        result)
        {
            RayHitTestResult rayResult = result as RayHitTestResult;
            if (rayResult != null && rayResult.ModelHit != null)
            {
                selectedModel = rayResult.VisualHit as ModelVisual3D;
                return HitTestResultBehavior.Stop;
            }
            return HitTestResultBehavior.Continue;
        }
        //скролл
        private void Wheel(object sender, MouseWheelEventArgs e)
        {
            // Приближение и отдаление камеры на скролл колесиком
            double zoomDelta = e.Delta * 0.001;
            cameraPosition += cameraLookDirection * zoomDelta;
            UpdateCamera();
        }
        private void MLBDown(object sender, MouseButtonEventArgs e)
        {
            // Запоминаем начальную позицию мыши при нажатии ЛКМ
            previousMousePosition = e.GetPosition(this);
        }
        //перемещение лкм
        private void MLBUp(object sender, MouseButtonEventArgs e)
        {
            // Сбрасываем начальную позицию мыши при отпускании ЛКМ
            previousMousePosition = new System.Windows.Point();
        }
        private void MRBDown(object sender, MouseButtonEventArgs e)
        {
            // Запоминаем начальную позицию мыши при нажатии ПКМ
            previousMousePosition = e.GetPosition(this);
        }
        //перемещение пкм
        private void MRBUp(object sender, MouseButtonEventArgs e)
        {
            // Сбрасываем начальную позицию мыши при отпускании ПКМ
            previousMousePosition = new System.Windows.Point();
        }
        //перемещение света
        private void MoveLight(Light light, double deltaX, double deltaY)
        {
            double moveFactor = 0.01;
            if (light is DirectionalLight directionalLight)
            {
                // Перемещаем направленный свет
                directionalLight.Direction = new Vector3D(
                directionalLight.Direction.X + deltaX *
                moveFactor,
                directionalLight.Direction.Y - deltaY *
                moveFactor,
                directionalLight.Direction.Z
                );
            }
            else if (light is PointLight pointLight)
            {
                // Перемещаем точечный свет
                pointLight.Position = new Point3D(
                pointLight.Position.X + deltaX * moveFactor,
                pointLight.Position.Y - deltaY * moveFactor,
                pointLight.Position.Z
                );
            }
            else if (light is SpotLight spotLight)
            {
                // Перемещаем прожектор
                spotLight.Position = new Point3D(
                spotLight.Position.X + deltaX * moveFactor,
                spotLight.Position.Y - deltaY * moveFactor,
                spotLight.Position.Z
                );
            }
        }
        private void MMove(object sender, MouseEventArgs e)
        {
            System.Windows.Point currentMousePosition =
            e.GetPosition(this);
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                double deltaX = currentMousePosition.X -
            previousMousePosition.X;
                double deltaY = currentMousePosition.Y -
                previousMousePosition.Y;
                // Перемещение выделенной фигуры
                if (selectedModel != null)
                {
                    double moveFactor = 0.01;
                    Vector3D offset = new Vector3D(deltaX *
                    moveFactor, -deltaY * moveFactor, 0);
                    TranslateTransform3D transform =
                    selectedModel.Transform as TranslateTransform3D;
                    if (transform == null)
                    {
                        transform = new
                        TranslateTransform3D(offset);
                    }
                    else
                    {
                        transform.OffsetX += offset.X;
                        transform.OffsetY += offset.Y;
                    }
                    selectedModel.Transform = transform;
                }
                else if (movingDirectionalLight)
                {
                    MoveLight(DirectionalLight, deltaX, deltaY);
                }
                else if (movingPointLight)
                {
                    MoveLight(PointLight, deltaX, deltaY);
                }
                else if (movingSpotLight)
                {
                    MoveLight(SpotLight, deltaX, deltaY);
                }
                else
                {
                    // Перемещение камеры
                    double moveFactor = 0.007;
                    Vector3D strafe =
                    Vector3D.CrossProduct(cameraLookDirection, cameraUpDirection) * -deltaX *
                    moveFactor;
                    Vector3D elevation = cameraUpDirection * -deltaY *
                    moveFactor;
                    cameraPosition += strafe - elevation;
                    UpdateCamera();
                }
                previousMousePosition = currentMousePosition;
            }
            else if (e.RightButton == MouseButtonState.Pressed)
            {
                double deltaX = currentMousePosition.X -
                previousMousePosition.X;
                RotateCamera(deltaX * 0.1);
                previousMousePosition = currentMousePosition;
                UpdateCamera();
            }
        }
private void InitializeCamera()
        {
            // Инициализация камеры
            camera = new PerspectiveCamera();
            camera.Position = new Point3D(0, 0.5, 7); // Начальная позиция камеры
camera.LookDirection = new Vector3D(0, 0, -1); // Начальное направление взгляда камеры
camera.UpDirection = new Vector3D(0, 1, 0); // Направление вверх
viewport.Camera = camera;
            // Начальное положение и направление камеры для обновления
            cameraPosition = camera.Position;
            cameraLookDirection = camera.LookDirection;
            cameraUpDirection = camera.UpDirection;
            // Привязка событий мыши и клавиатуры для управления камерой
            KeyDown += Keys;
        }
        #region управление камерой
        private void ResetCameraButton(object sender, RoutedEventArgs e)
        {
            // Устанавливаем начальное положение камеры
            camera.Position = new Point3D(0, 0.5, 7); // Начальная позиция  камеры
        camera.LookDirection = new Vector3D(0, 0, -1); // Начальное направление взгляда камеры
        camera.UpDirection = new Vector3D(0, 1, 0); // Направление  вверх
            // Обновляем значения переменных камеры
            cameraPosition = camera.Position;
            cameraLookDirection = camera.LookDirection;
            cameraUpDirection = camera.UpDirection;
        }
        private void Keys(object sender, KeyEventArgs e)
        {
            // Обработка события нажатия клавиши для перемещения камеры
            switch (e.Key)
            {
                case Key.W:
                    cameraPosition += cameraLookDirection *
                    cameraSpeed;
                    break;
                case Key.S:
                    cameraPosition -= cameraLookDirection *
                    cameraSpeed;
                    break;
                case Key.A:
                    cameraPosition -=
                    Vector3D.CrossProduct(cameraLookDirection, new Vector3D(0, 1, 0)) * cameraSpeed;
                    break;
                case Key.D:
                    cameraPosition +=
                    Vector3D.CrossProduct(cameraLookDirection, new Vector3D(0, 1, 0)) * cameraSpeed;
            break;
                case Key.Q:
                    RotateCamera(-rotationSpeed);
                    break;
                case Key.E:
                    RotateCamera(rotationSpeed);
                    break;
                case Key.R:
                    cameraPosition += cameraUpDirection * cameraSpeed;
                    break;
                case Key.F:
                    cameraPosition -= cameraUpDirection * cameraSpeed;
                    break;
                case Key.Escape: // Отмена выделения фигуры
                    selectedModel = null;
                    break;
            }
            UpdateCamera();
        }
        private void UpdateCamera()
        {
            camera.Position = cameraPosition;
            camera.LookDirection = cameraLookDirection;
        }
        private void RotateCamera(double angle)
        {
            double radians = angle * Math.PI / 180.0;
            Matrix3D rotationMatrix = new Matrix3D();
            rotationMatrix.Rotate(new
            System.Windows.Media.Media3D.Quaternion(cameraUpDirection, angle));
            cameraLookDirection =
            rotationMatrix.Transform(cameraLookDirection);
        }
        #endregion
        private void InitializeViewport()
        {
            //AddCube();
            AddSphere();
            AddSphere2();
            //AddCylinder();
            AddPlane(10);
            DisplayShapes();
            AddAxes();
        }
        #region Sphere
        private void AddSphere()
        {
            MeshGeometry3D mesh = new MeshGeometry3D();
            int tDiv = 32;
            int pDiv = 16;
            double radius = 0.5;
            double offsetY = 0.5;
            double offsetX = 1.5;
        for (int t = 0; t < tDiv; t++)
            {
                double theta = Math.PI * t / tDiv;
                double sinTheta = Math.Sin(theta);
                double cosTheta = Math.Cos(theta);
                for (int p = 0; p <= pDiv; p++)
                {
                    double phi = 2 * Math.PI * p / pDiv;
                    double x = radius * sinTheta * Math.Cos(phi) +
                    offsetX;
                    double y = radius * cosTheta + offsetY;
                    double z = radius * sinTheta * Math.Sin(phi);
                    Vector3D normal = new Vector3D(x - offsetX, y -
                    offsetY, z);
                    normal.Normalize(); // Нормализуем вектор, чтобы   получить единичный вектор
                mesh.Positions.Add(new Point3D(x, y, z));
                    mesh.Normals.Add(normal);
                }
            }
            for (int t = 0; t < tDiv; t++)
            {
                for (int p = 0; p < pDiv; p++)
                {
                    int a = (t * (pDiv + 1)) + p;
                    int b = a + pDiv + 1;
                    mesh.TriangleIndices.Add(a);
                    mesh.TriangleIndices.Add(b);
                    mesh.TriangleIndices.Add(a + 1);
                    mesh.TriangleIndices.Add(b);
                    mesh.TriangleIndices.Add(b + 1);
                    mesh.TriangleIndices.Add(a + 1);
                }
            }
            GeometryModel3D sphereModel = new GeometryModel3D
            {
                Geometry = mesh,
                Material = new DiffuseMaterial(new
            SolidColorBrush(Colors.Blue))
            };
            ModelVisual3D model = new ModelVisual3D
            {
                Content =
            sphereModel
            };
            viewport.Children.Add(model);
            // Добавляем фигуру в список и обновляем TextBlock
            shapes.Add(new Shape { Name = "Sphere" });
            DisplayShapes();
        }
        #endregion
        #region Sphere
        private void AddSphere2()
{
MeshGeometry3D mesh = new MeshGeometry3D();
        int tDiv = 32;
        int pDiv = 16;
        double radius = 0.5;
        double offsetY = 0.5;
for (int t = 0; t<tDiv; t++)
{
double theta = Math.PI * t / tDiv;
        double sinTheta = Math.Sin(theta);
        double cosTheta = Math.Cos(theta);
for (int p = 0; p <= pDiv; p++)
{
double phi = 2 * Math.PI * p / pDiv;
        double x = radius * sinTheta * Math.Cos(phi);
        double y = radius * cosTheta + offsetY;
        double z = radius * sinTheta * Math.Sin(phi);
        Vector3D normal = new Vector3D(x, y, z);
        normal.Normalize(); // Нормализуем вектор, чтобы получить единичный вектор
mesh.Positions.Add(new Point3D(x, y, z));
mesh.Normals.Add(normal);
}
}
for (int t = 0; t < tDiv; t++)
{
    for (int p = 0; p < pDiv; p++)
    {
        int a = (t * (pDiv + 1)) + p;
        int b = a + pDiv + 1;
        mesh.TriangleIndices.Add(a);
        mesh.TriangleIndices.Add(b);
        mesh.TriangleIndices.Add(a + 1);
        mesh.TriangleIndices.Add(b);
        mesh.TriangleIndices.Add(b + 1);
        mesh.TriangleIndices.Add(a + 1);
    }
}
GeometryModel3D sphereModel = new GeometryModel3D
{
    Geometry = mesh,
    Material = new DiffuseMaterial(new
SolidColorBrush(Colors.Red))
};
ModelVisual3D model = new ModelVisual3D
{
    Content =
sphereModel
};
viewport.Children.Add(model);
// Добавляем фигуру в список и обновляем TextBlock
shapes.Add(new Shape { Name = "Sphere" });
DisplayShapes();
}
#endregion
#region light
private void Ligthing()
{
    DirectionalLight.Color = Colors.Transparent;
    PointLight.Color = Colors.Transparent;
    SpotLight.Color = Colors.Transparent;
}
private void DirectionalLightButton(object sender, RoutedEventArgs
e)
{
    Ligthing();
    DirectionalLight.Color = Colors.White;
}
private void PointLightButton(object sender, RoutedEventArgs e)
{
    Ligthing();
    PointLight.Color = Colors.DeepSkyBlue;
}
private void SpotLightButton(object sender, RoutedEventArgs e)
{
    Ligthing();
    SpotLight.Color = Colors.Yellow;
}
private void LightsOff(object sender, RoutedEventArgs e)
{
    Ligthing();
}
#endregion
private class PopupWindow
{
    public PopupWindow()
    {
    }
    internal void ShowDialog()
    {
        throw new NotImplementedException();
    }
}
private void Button_Click(object sender, RoutedEventArgs e)
{
    ProgramDescriptionWindow descriptionWindow = new
    ProgramDescriptionWindow();
    descriptionWindow.ShowDialog();
}
public class Shape
{
    public string Name { get; set; }
}
// Поле для хранения списка фигур
private List<Shape> shapes = new List<Shape>();
private void DisplayShapes()
{
    StringBuilder sb = new StringBuilder("Список фигур:\n");
    foreach (var shape in shapes)
    {
        sb.AppendLine(shape.Name);
    }
    ShapesTextBlock.Text = sb.ToString();
}
#region плоскость
private void AddPlane(double size)
{
    MeshGeometry3D mesh = new MeshGeometry3D();
    // Определяем вершины плоскости
    Point3DCollection positions = new Point3DCollection
{
new Point3D(-size / 2, 0, -size / 2),
new Point3D(size / 2, 0, -size / 2),
new Point3D(size / 2, 0, size / 2),
new Point3D(-size / 2, 0, size / 2)
};
    Vector3DCollection normals = new Vector3DCollection
{
new Vector3D(0, 1, 0), // Вверх
 new Vector3D(0, 1, 0),
 new Vector3D(0, 1, 0),
 new Vector3D(0, 1, 0),
 new Vector3D(0, -1, 0), // Вниз
 new Vector3D(0, -1, 0),
 new Vector3D(0, -1, 0),
 new Vector3D(0, -1, 0)
 };
    // Определяем треугольники плоскости
    Int32Collection triangleIndices = new Int32Collection
{
0, 1, 2,
2, 3, 0,
2, 1, 0,
0, 3, 2
};
    mesh.Positions = positions;
    mesh.Normals = normals;
    mesh.TriangleIndices = triangleIndices;
    GeometryModel3D planeModel = new GeometryModel3D
    {
        Geometry = mesh,
        Material = new DiffuseMaterial(new
    SolidColorBrush(Colors.Gray))
    };
    ModelVisual3D model = new ModelVisual3D
    {
        Content = planeModel
    };
    viewport.Children.Add(model);
}
#endregion
#region Axes
private void AddAxes()
{
    // X
    xAxis = new ArrowVisual3D
    {
        Diameter = 0.01,
        Point2 = new Point3D(3, 0, 0),
        Visible = false,
        Fill = System.Windows.Media.Brushes.Red
    };
    // Y
    yAxis = new ArrowVisual3D
    {
        Diameter = 0.01,
        Point2 = new Point3D(0, 2, 0),
        Visible = false,
        Fill = System.Windows.Media.Brushes.Green
    };
    // Z
    zAxis = new ArrowVisual3D
    {
        Diameter = 0.01,
        Point2 = new Point3D(0, 0, 2),
        Visible = false,
        Fill = System.Windows.Media.Brushes.Blue
    };
    // группировка всех осей
    var axisModel = new ModelVisual3D();
    axisModel.Children.Add(xAxis);
    axisModel.Children.Add(yAxis);
    axisModel.Children.Add(zAxis);
    viewport.Children.Add(axisModel);
}
private void ShowAxes()
{
    // видимость осей
    xAxis.Visible = true;
    yAxis.Visible = true;
    zAxis.Visible = true;
    // обновить отображение
    viewport.InvalidateVisual();
}
private void HideAxes()
{
    // невидимость осей
    xAxis.Visible = false;
    yAxis.Visible = false;
zAxis.Visible = false;
    // обновить отображение
    viewport.InvalidateVisual();
}
private void AxisCheckBox_Checked(object sender, RoutedEventArgs e)
{
    ShowAxes();
}
private void AxisCheckBox_Unchecked(object sender, RoutedEventArgs
e)
{
    HideAxes();
}
#endregion
}
#endregion
public class LightPositions : INotifyPropertyChanged
{
    private Point3D _directionalLightDirection = new Point3D(-2, 1, 4);
    public Point3D DirectionalLightDirection
    {
        get { return _directionalLightDirection; }
        set
        {
            if (_directionalLightDirection != value)
            {
                _directionalLightDirection = value;
                OnPropertyChanged(nameof(DirectionalLightDirection));
            }
        }
    }
    private Point3D _pointLightPosition = new Point3D(4, 2, 3);
    public Point3D PointLightPosition
    {
        get { return _pointLightPosition; }
        set
        {
            if (_pointLightPosition != value)
            {
                _pointLightPosition = value;
                OnPropertyChanged(nameof(PointLightPosition));
            }
        }
    }
    private Point3D _spotLightPosition = new Point3D(4, 6, 1);
    public Point3D SpotLightPosition
    {
        get { return _spotLightPosition; }
        set
        {
            if (_spotLightPosition != value)
            {
            _spotLightPosition = value;
                OnPropertyChanged(nameof(SpotLightPosition));
            }
        }
    }
    public event PropertyChangedEventHandler PropertyChanged;
    protected void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new
        PropertyChangedEventArgs(propertyName));
    }
}
}