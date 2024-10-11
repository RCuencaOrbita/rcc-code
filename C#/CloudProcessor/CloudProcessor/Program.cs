using System.Globalization;
using System.Numerics;

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
        static double tamanoCubo = 11.5; // Tamaño del cubo (mismas unidades que las coordenadas)

        static void Main(string[] args)
        {

            System.IO.DirectoryInfo dir = new DirectoryInfo("C:\\\\data");
            foreach (var archivo in dir.EnumerateFiles())
            {
                try
                {
                    // Cargar la nube de puntos desde un archivo

                    string[] lineas = File.ReadAllLines(archivo.FullName);

                    // Inicializar los valores mínimos
                    double xMin = double.MaxValue;
                    double yMin = double.MaxValue;
                    double zMin = double.MaxValue;

                    double xMax = double.MinValue;
                    double yMax = double.MinValue;
                    double zMax = double.MinValue;
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
                    var puntosContacto = new List<Vector3>()
                        {
                            ExtraerPunto(cabecera[4] ),
                            ExtraerPunto(cabecera[6] ),
                            ExtraerPunto(cabecera[8] ),
                            ExtraerPunto(cabecera[10] )
                        };
                    puntosContacto = CompletarPuntos(puntosContacto);
                    //actualizar valores minimos con los puntos de contacto  
                    if (puntosContacto.Any(punto => punto.X < xMin))
                    {
                        xMin = puntosContacto.Min(punto => punto.X);
                    }
                    if (puntosContacto.Any(punto => punto.Y < yMin))
                    {
                        yMin = puntosContacto.Min(punto => punto.Y);
                    }
                    if (puntosContacto.Any(punto => punto.Z < zMin))
                    {
                        zMin = puntosContacto.Min(punto => punto.Z);
                    }
                    Vector3 mins = new Vector3((float)xMin, (float)yMin, (float)zMin);
                    //Valores máximos
                    if (puntosContacto.Any(punto => punto.X > xMax))
                    {
                        xMax = puntosContacto.Max(punto => punto.X);
                    }
                    if (puntosContacto.Any(punto => punto.Y > yMax))
                    {
                        yMax = puntosContacto.Max(punto => punto.Y);
                    }
                    if (puntosContacto.Any(punto => punto.Z > zMax))
                    {
                        zMax = puntosContacto.Max(punto => punto.Z);
                    }
                    //tamaños
                    xTam = xMax - xMin;
                    yTam = yMax - yMin;
                    zTam = zMax - zMin;

                    // Recorrer cada línea (cada punto) en el archivo
                    foreach (string linea in cuerpo)
                    {
                        // Separar las coordenadas X, Y, Z (asumimos que están separadas por punto y coma)
                        string[] coordenadas = linea.Split(';');
                        if (coordenadas.Length != 3) continue;

                        // Convertir las coordenadas a double
                        float x = float.Parse(coordenadas[0].Replace(",", "."), CultureInfo.InvariantCulture);
                        float y = float.Parse(coordenadas[1].Replace(",", "."), CultureInfo.InvariantCulture);
                        float z = float.Parse(coordenadas[2].Replace(",", "."), CultureInfo.InvariantCulture);
                        //trasladar coordenadas a origen del cubo
                        Vector3 punto = new Vector3(x, y, z);
                        punto = EscalarPuntos(punto, mins);
                        if (punto.X < 0 || punto.Y < 0 || punto.Z < 0)
                        {
                            throw new Exception("Alguna coordenada menor que cero");
                        }
                        // Asegurarse de que los índices están dentro de los límites de la matriz
                        if (punto.X >= 0 && punto.X < dimensionMatriz && punto.Y >= 0 && punto.Y < dimensionMatriz && punto.Z >= 0 && punto.Z < dimensionMatriz)
                        {
                            matriz3D[(int)punto.X, (int)punto.Y, (int)punto.Z] = 1;
                        }
                        else
                        {
                            throw new Exception("Coordenadas fuera de los limites de la matriz.");
                        }
                    }

                    try
                    {
                        //escalar los 4 puntos de puntosContacto con EscalarPuntos
                        puntosContacto = puntosContacto.Select(punto => EscalarPuntos(punto, mins)).ToList();
                        //escribir la linea de puntos
                        string puntosFormateados = string.Join("-", puntosContacto.Select(punto => PuntoACadena(punto)));
                        // Guardar la matriz resultante en un archivo de salida
                        using (StreamWriter salida = new StreamWriter("C:\\\\Clouds\\" + puntosFormateados + "-" + archivo.Name))
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
                    catch (Exception ex)
                    {

                    }

                }
                catch (Exception ex)
                {

                }


            }


        }

        public static List<Vector3> CompletarPuntos(List<Vector3> puntos)
        {
            // Asegurarse de que la lista tenga exactamente 4 puntos
            if (puntos.Count != 4)
            {
                throw new ArgumentException("La lista debe contener exactamente 4 puntos.");
            }

            // Encontrar el índice del punto (0, 0, 0) si existe
            int indiceFaltante = puntos.FindIndex(p => p == Vector3.Zero);

            // Si no hay ningún punto faltante (0, 0, 0), no hay nada que calcular
            if (indiceFaltante == -1)
            {
                return puntos;  // No se requiere cálculo
            }

            // Identificar los tres puntos conocidos
            Vector3 frontLeft = puntos[0];
            Vector3 frontRight = puntos[1];
            Vector3 rearLeft = puntos[2];
            Vector3 rearRight = puntos[3];

            // Calcular el punto faltante en función de la disposición
            switch (indiceFaltante)
            {
                case 0:  // Faltante: Delante izquierda (Front-Left)
                    frontLeft = frontRight + rearLeft - rearRight;
                    break;
                case 1:  // Faltante: Delante derecha (Front-Right)
                    frontRight = frontLeft + rearRight - rearLeft;
                    break;
                case 2:  // Faltante: Detrás izquierda (Rear-Left)
                    rearLeft = rearRight + frontLeft - frontRight;
                    break;
                case 3:  // Faltante: Detrás derecha (Rear-Right)
                    rearRight = rearLeft + frontRight - frontLeft;
                    break;
            }

            // Reemplazar el punto faltante en la lista original
            puntos[0] = frontLeft;
            puntos[1] = frontRight;
            puntos[2] = rearLeft;
            puntos[3] = rearRight;

            // Retornar la lista con el punto calculado
            return puntos;
        }
        private static Vector3 ExtraerPunto(string linea)
        {
            Vector3 punto = new Vector3(0, 0, 0);
            if (linea.Trim() == "0;0;0") return punto;
            var puntos = linea.Split(';');
            punto.X = float.Parse(puntos[0].Replace(",", "."), CultureInfo.InvariantCulture);
            punto.Y = float.Parse(puntos[1].Replace(",", "."), CultureInfo.InvariantCulture);
            punto.Z = float.Parse(puntos[2].Replace(",", "."), CultureInfo.InvariantCulture);
            return punto;
        }
        private static Vector3 EscalarPuntos(Vector3 punto, Vector3 minimo)
        {
            punto.X -= minimo.X;
            punto.Y -= minimo.Y;
            punto.Z -= minimo.Z;
            punto.X = (int)Math.Floor((punto.X / tamanoCubo) * dimensionMatriz);
            punto.Y = (int)Math.Floor((punto.Y / tamanoCubo) * dimensionMatriz);
            punto.Z = (int)Math.Floor((punto.Z / tamanoCubo) * dimensionMatriz);
            if (punto.X < 0 || punto.Y < 0 || punto.Z < 0)
            {
                throw new Exception("Indices de punto de contacto menor que cero");
            }
            if (punto.X >= dimensionMatriz || punto.Y >= dimensionMatriz || punto.Z >= dimensionMatriz)
            {
                Console.WriteLine("Dimensiones incorrectas");
            }
            return punto;
        }
        private static string PuntoACadena(Vector3 punto)
        {
            return $"{punto.X}_{punto.Y}_{punto.Z}";
        }
    }
}
