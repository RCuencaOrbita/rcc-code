using System.Globalization;

namespace CloudProcessor
{
    using System;
    using System.IO;
    using System.Globalization;
    using System.Reflection.Metadata;
    using System.Numerics;

    class Program
    {
        // Parámetros de la matriz 3D
        static int dimensionMatriz = 30; // Ajusta según la resolución de la matriz deseada
        static double tamanoCubo = 10; // Tamaño del cubo (mismas unidades que las coordenadas)

        static void Main(string[] args)
        {

            System.IO.DirectoryInfo dir = new DirectoryInfo("C:\\\\data");
            foreach (var archivo in dir.EnumerateFiles())
            {
                // Cargar la nube de puntos desde un archivo

                string[] lineas = File.ReadAllLines(archivo.FullName);

                // Inicializar los valores mínimos
                double xMin = double.MaxValue;
                double yMin = double.MaxValue;
                double zMin = double.MaxValue;

                double xMax = 0;
                double yMax = 0;
                double zMax = 0;
                // Crear la matriz 3D de enteros para contar puntos
                int[,,] matriz3D = new int[dimensionMatriz, dimensionMatriz, dimensionMatriz];

                var cuerpo = lineas.Skip(12);

                var xTam = 0.0;
                var yTam = 0.0;
                var zTam = 0.0;
                foreach (string linea in cuerpo)
                {
                    string[] coordenadas = linea.Split(';');
                    if (coordenadas.Length != 3) continue;

                    double x = double.Parse(coordenadas[0].Replace(",", "."), CultureInfo.InvariantCulture);
                    double y = double.Parse(coordenadas[1].Replace(",", "."), CultureInfo.InvariantCulture);
                    double z = double.Parse(coordenadas[2].Replace(",", "."), CultureInfo.InvariantCulture);

                    // Actualizar los valores mínimos
                    if (x < xMin) xMin = x;
                    if (y < yMin) yMin = y;
                    if (z < zMin) zMin = z;
                    // Actualizar los valores más altos
                    if (x > xMax) xMax = x;
                    if (y > yMax) yMax = y;
                    if (z > zMax) zMax = z;

                    xTam = xMax - xMin;
                    yTam = yMax - yMin;
                    zTam = zMax - zMin;
                }



                // Recorrer cada línea (cada punto) en el archivo
                foreach (string linea in cuerpo)
                {
                    // Separar las coordenadas X, Y, Z (asumimos que están separadas por punto y coma)
                    string[] coordenadas = linea.Split(';');
                    if (coordenadas.Length != 3) continue;

                    // Convertir las coordenadas a double
                    double x = double.Parse(coordenadas[0].Replace(",", "."), CultureInfo.InvariantCulture);
                    double y = double.Parse(coordenadas[1].Replace(",", "."), CultureInfo.InvariantCulture);
                    double z = double.Parse(coordenadas[2].Replace(",", "."), CultureInfo.InvariantCulture);
                    //trasladar coordenadas a origen del cubo
                    x = x - xMin;
                    y = y - yMin;
                    z = z - zMin;
                    // Convertir las coordenadas al índice de la matriz (discretización)
                    int i = (int)((x / tamanoCubo) * dimensionMatriz);
                    int j = (int)((y / tamanoCubo) * dimensionMatriz);
                    int k = (int)((z / tamanoCubo) * dimensionMatriz);

                    // Asegurarse de que los índices están dentro de los límites de la matriz
                    if (i >= 0 && i < dimensionMatriz && j >= 0 && j < dimensionMatriz && k >= 0 && k < dimensionMatriz)
                    {
                        matriz3D[i, j, k] = 1; // Incrementar el contador en esa celda de la matriz
                    }
                    else
                    {
                        throw new Exception("Coordenadas fuera de los limites de la matriz.");
                    }
                }
                string[] cabecera = lineas.Take(12).ToArray();
                // Tamaño fijo de cabecera
                //0 Cubo
                //1 Centro: (131.24, 3.98, 25.83)
                //2 Contactos
                //3 1:
                //4 2,083214; -2,01517; -2,065616
                //5 2:
                //6 2,158125; 2,344577; -1,583078
                //7 3:
                //8 -2,737509; -2,011427; -1,350888
                //9 4:
                //10 0; 0; 0
                Vector3 mins = new Vector3((float)xMin, (float)yMin, (float)zMin);
                string puntosFormateados = ExtraerPuntos(cabecera[4], mins) + "-" +
                                            ExtraerPuntos(cabecera[6], mins) + "-" +
                                            ExtraerPuntos(cabecera[8], mins) + "-" +
                                            ExtraerPuntos(cabecera[10], mins) + "-";
                // Guardar la matriz resultante en un archivo de salida
                using (StreamWriter salida = new StreamWriter("C:\\\\Clouds\\" + puntosFormateados + archivo.Name + "." + archivo.Extension))
                {
                    for (int i = 0; i < dimensionMatriz; i++)
                    {
                        for (int j = 0; j < dimensionMatriz; j++)
                        {
                            for (int k = 0; k < dimensionMatriz; k++)
                            {
                                if (matriz3D[i, j, k] > 0)
                                {
                                    salida.WriteLine($"{i},{j},{k},{matriz3D[i, j, k]}");
                                }
                            }
                        }
                    }
                }

                Console.WriteLine("Proceso completado. Matriz 3D guardada en 'matriz3D.txt'.");

            }


        }

        private static string ExtraerPuntos(string linea, Vector3 minimos)
        {
            if (linea.Trim() == "0;0;0") return "";
            var puntos = linea.Split(';');
            double x = double.Parse(puntos[0].Replace(",", "."), CultureInfo.InvariantCulture);
            double y = double.Parse(puntos[1].Replace(",", "."), CultureInfo.InvariantCulture);
            double z = double.Parse(puntos[2].Replace(",", "."), CultureInfo.InvariantCulture);
            x -= minimos.X;
            y -= minimos.Y;
            z -= minimos.Z;
            int i = (int)((x / tamanoCubo) * dimensionMatriz);
            int j = (int)((y / tamanoCubo) * dimensionMatriz);
            int k = (int)((z / tamanoCubo) * dimensionMatriz);
            return i.ToString() + "_" + j.ToString() + "_" + k.ToString();
        }
    }
}
