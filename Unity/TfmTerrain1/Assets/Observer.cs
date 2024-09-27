using UnityEngine;
using System.Collections.Generic;
using System;
using System.IO;
using UnityEngine.InputSystem.HID;
using System.Drawing;
using System.Linq;

//public class Observer : MonoBehaviour
//{
//    public float cubeSize = 6.0f;
//    public int pointsPerAxis = 20;  // N�mero de puntos a muestrear por eje
//    public LayerMask terrainLayer;   // Capa que representa el terreno

//    private List<Vector3> pointCloud = new List<Vector3>();
//    private Terrain terrain; // Referencia al objeto Terrain

//    void Start()
//    {
//        // Encuentra el objeto Terrain en la escena
//        terrain = GameObject.Find("Terrain").GetComponent<Terrain>();

//        if (terrain == null)
//        {
//            Debug.LogError("No se encontr� un objeto Terrain en la escena.");
//        }
//    }
//    float lastObservation = 0;
//    void Update()
//    {
//        if (Time.time - lastObservation < 10)
//        {
//            return;
//        }
//        lastObservation = Time.time;
//        if (terrain == null)
//        {
//            return;
//        }

//        // Actualiza la posici�n del cubo a la posici�n actual del veh�culo
//        Vector3 center = transform.position;

//        // Limpia la nube de puntos anterior
//        pointCloud.Clear();

//        // Recorre el volumen definido por el cubo para realizar muestreos
//        float halfSize = cubeSize / 2.0f;
//        float stepSize = cubeSize / pointsPerAxis;

//        for (int x = 0; x < pointsPerAxis; x++)
//        {
//            for (int y = 0; y < pointsPerAxis; y++)
//            {
//                for (int z = 0; z < pointsPerAxis; z++)
//                {
//                    // Calcula la posici�n de cada punto de muestreo dentro del cubo
//                    Vector3 point = new Vector3(
//                        center.x + (x * stepSize - halfSize),
//                        center.y + (y * stepSize - halfSize),
//                        center.z + (z * stepSize - halfSize)
//                    );

//                    // Ajusta la posici�n Y seg�n la altura del terreno en el punto X,Z
//                    point.y = terrain.SampleHeight(point) + terrain.GetPosition().y;

//                    // Agrega el punto a la nube de puntos si est� dentro del terreno
//                    pointCloud.Add(point);
//                }
//            }
//        }

//        // Opcional: Guardar la nube de puntos en un archivo o utilizarla de alguna manera
//        SavePointCloud();
//    }

//    void SavePointCloud()
//    {
//        // Ejemplo de c�mo guardar la nube de puntos en un archivo (formato XYZ)
//        string filePath = "c:\\data\\pointCloud" + DateTime.Now.ToString("yyyyMMddHHmmssfff") + ".txt";
//        using (System.IO.StreamWriter file = new System.IO.StreamWriter(filePath))
//        {
//            foreach (Vector3 point in pointCloud)
//            {
//                file.WriteLine($"{point.x} {point.y} {point.z}");
//            }
//        }
//    }

//    void OnDrawGizmos()
//    {
//        // Dibujar el cubo de observaci�n en la escena para visualizar el �rea de muestreo
//        Gizmos.color = Color.green;
//        Gizmos.DrawWireCube(transform.position, Vector3.one * cubeSize);

//        // Dibujar los puntos de la nube de puntos
//        Gizmos.color = Color.red;
//        foreach (Vector3 point in pointCloud)
//        {
//            Gizmos.DrawSphere(point, 0.1f);
//        }
//    }
//}
//##############################################################


public class Observer : MonoBehaviour
{
    public GameObject vehicle;
    public LayerMask terrainLayer;
    public float lidarDistance = 10f;
    public int rayCount = 100;
    public float cubeSize = 3f;
    public float horizontalStep = 0.5f; // Ajusta este valor para cambiar la resoluci�n horizontal
    public float verticalStep = 0.5f;  // Ajusta este valor para cambiar la resoluci�n vertical
    public float horizontalRange = 10f; // Rango horizontal del barrido
    public float verticalRange = 5f;  // Rango vertical del barrido
    public WheelCollider[] wheelColliders; // Array de WheelColliders

    private List<Vector3> particles;
    private float lastObservation = 0f;

    void Start()
    {

        particles = new List<Vector3>();

    }


    public List<Vector3> GetContactPoints()
    {
        List<Vector3> contacts = new List<Vector3>();
        foreach (WheelCollider wheel in wheelColliders)
        {
            // Obtener informaci�n sobre el contacto
            WheelHit hit;
            if (wheel.GetGroundHit(out hit))
            {
                // Verificar si el contacto es con la capa "TERRENO"
                if ((1 << hit.collider.gameObject.layer) == terrainLayer.value)
                {
                    // Obtener el punto de contacto
                    //Vector3 contactPoint = wheel.transform.TransformPoint(hit.point);
                    contacts.Add(hit.point);
                    // Hacer algo con el punto de contacto (por ejemplo, mostrarlo en la consola)
                    //Debug.Log("Punto de contacto: " + contactPoint);
                }
            }
            else
            {
                contacts.Add(Vector3.zero);
            }
        }
        return contacts;
    }
    void Update()
    {
        if (Time.time - lastObservation < 0.1)
        {
            return;
        }
        var contacts = GetContactPoints();
        if (contacts.Where(x => x != Vector3.zero).Count() < 3)
        {
            return;
        }
        lastObservation = Time.time; // Update observation time
        particles.Clear();
        // Calcular la posici�n del LiDAR
        Vector3 lidarPosition = vehicle.transform.position + vehicle.transform.forward * -lidarDistance;
        lidarPosition.y = lidarPosition.y + 10;

        // Punto de inicio del barrido
        Vector3 startPoint = lidarPosition + vehicle.transform.right * -horizontalRange / 2 + vehicle.transform.up * verticalRange / 2;
        Vector3 vehiclePosition = vehicle.transform.position;
        // Definir los ejes locales del cubo de observaci�n:
        // - forward sigue siendo la direcci�n del veh�culo
        // - right est� en el plano horizontal, perpendicular al forward
        // - up es el eje global 'up' (Vector3.up)
        Vector3 vehicleForward = vehicle.transform.forward;
        vehicleForward.y = 0; // Hacer que el 'forward' sea horizontal
        vehicleForward.Normalize();
        Vector3 vehicleRight = Vector3.Cross(Vector3.up, vehicleForward); // Right perpendicular a forward y up

        // proyectar los puntos de contacto en el eje de coordenadas del cubo
        for (int i = 0; i < contacts.Count; i++)
        {
            if (contacts[i]== Vector3.zero)
            {
                continue;
            }
            contacts[i] = contacts[i] - vehiclePosition;
            // Proyectar el punto de impacto en los ejes locales modificados
            float forwardProjection = Vector3.Dot(contacts[i], vehicleForward);
            float rightProjection = Vector3.Dot(contacts[i], vehicleRight);
            float upProjection = Vector3.Dot(contacts[i], Vector3.up); // Usa el eje global 'up'
            contacts[i] = new Vector3(forwardProjection, rightProjection, upProjection);

        }




        // Iterar sobre cada punto del barrido       
        for (float y = verticalRange; y >= -verticalRange; y -= verticalStep)
        {
            for (float x = -horizontalRange; x <= horizontalRange; x += horizontalStep)
            {

                Vector3 direction = vehicle.transform.forward + vehicle.transform.right * ((-horizontalRange / 2) + x) + vehicle.transform.up * y;// ((verticalRange / 2) + y);

                // Normalizar la direcci�n para asegurar que la magnitud sea 1
                direction.Normalize();

                Ray ray = new Ray(lidarPosition, direction * 100);
                //PARA VER LOS RAYOSDebug.DrawRay(lidarPosition, direction * 100, Color.red, 5);

                if (Physics.Raycast(ray, out RaycastHit hit, 200, terrainLayer))
                {
                    // Obtener la posici�n del impacto 
                    Vector3 hitPoint = hit.point;


                    // Calcular el vector de diferencia entre el punto de impacto y el veh�culo
                    Vector3 localHitPoint = hitPoint - vehiclePosition;

                    // Proyectar el punto de impacto en los ejes locales modificados
                    float forwardProjection = Vector3.Dot(localHitPoint, vehicleForward);
                    float rightProjection = Vector3.Dot(localHitPoint, vehicleRight);
                    float upProjection = Vector3.Dot(localHitPoint, Vector3.up); // Usa el eje global 'up'

                    // Comprobar si el punto proyectado est� dentro del cubo
                    if (Mathf.Abs(forwardProjection) <= cubeSize / 2 &&
                        Mathf.Abs(rightProjection) <= cubeSize / 2 &&
                        Mathf.Abs(upProjection) <= cubeSize / 2)
                    {
                        // particles.Add(hitPoint);
                        particles.Add(new Vector3(forwardProjection, rightProjection, upProjection));
                    }
                }
            }
        }
        // Save filtered points to text file
        SaveCubeToText(particles, contacts);
    }

    void SaveCubeToText(List<Vector3> points, List<Vector3> contacts)
    {
        string filePath = "c:\\data\\pointCloud" + DateTime.Now.ToString("yyyyMMddHHmmssfff") + ".txt";
        StreamWriter writer = new StreamWriter(filePath);

        // Escribir encabezado (optional)
        writer.WriteLine("Cubo");  // Assuming no cube ID needed
        writer.WriteLine("Centro: " + vehicle.transform.position);
        writer.WriteLine("Contactos");
        var i = 0;
        foreach (var contact in contacts)
        {
            i++;
            writer.WriteLine(i.ToString() + ":");
            writer.WriteLine(contact.x + ";" + contact.y + ";" + contact.z);
        }
        writer.WriteLine("Nube de puntos:");
        // Write only filtered points (within cube)
        foreach (Vector3 point in points)
        {
            writer.WriteLine(point.x + ";" + point.y + ";" + point.z);
        }

        writer.Close();
    }
}
//##############################################################
//using UnityEngine;
//using System.Collections.Generic;
//using System;

//public class Observer : MonoBehaviour
//{
//    public float cubeSize = 100.0f;
//    public int pointsPerAxis = 20;  // N�mero de puntos a muestrear por eje
//    public LayerMask terrainLayer;   // Capa que representa el terreno

//    private List<Vector3> pointCloud = new List<Vector3>();
//    float lastObservation = 0;
//    void Update()
//    {
//        if (Time.time - lastObservation<10)
//        {
//            return;
//        }
//        lastObservation = Time.time;
//        Time.timeScale = 0;
//        // Actualiza la posici�n del cubo a la posici�n actual del veh�culo
//        Vector3 center = transform.position;

//        // Limpia la nube de puntos anterior
//        pointCloud.Clear();

//        // Recorre el volumen definido por el cubo para realizar muestreos
//        float halfSize = cubeSize / 2.0f;
//        float stepSize = cubeSize / pointsPerAxis;

//        for (int x = 0; x < pointsPerAxis; x++)
//        {
//            for (int y = 0; y < pointsPerAxis; y++)
//            {
//                for (int z = 0; z < pointsPerAxis; z++)
//                {
//                    // Calcula la posici�n de cada punto de muestreo dentro del cubo
//                    Vector3 point = new Vector3(
//                        center.x + (x * stepSize - halfSize),
//                        center.y + (y * stepSize - halfSize),
//                        center.z + (z * stepSize - halfSize)
//                    );

//                    // Realiza un Raycast desde arriba hacia abajo
//                    if (Physics.Raycast(point + Vector3.up * halfSize, Vector3.down, out RaycastHit hit, cubeSize, terrainLayer))
//                    {
//                        // Si el Raycast impacta el terreno, agrega el punto a la nube de puntos
//                        pointCloud.Add(hit.point);
//                    }
//                }
//            }
//        }
//        //System.Threading.Thread.Sleep(10000);
//        // Opcional: Guardar la nube de puntos en un archivo o utilizarla de alguna manera
//        SavePointCloud();
//        Time.timeScale = 1;
//    } 

//    void SavePointCloud()
//    {
//        // Ejemplo de c�mo guardar la nube de puntos en un archivo (formato XYZ)
//        string filePath =  "c:\\data\\pointCloud"+DateTime.Now.ToString("yyyyMMddHHmmssfff")+".txt";
//        using (System.IO.StreamWriter file = new System.IO.StreamWriter(filePath))
//        {
//            foreach (Vector3 point in pointCloud)
//            {
//                file.WriteLine($"{point.x} {point.y} {point.z}");
//            }
//        }
//    }

//    void OnDrawGizmos()
//    {
//        // Dibujar el cubo de observaci�n en la escena para visualizar el �rea de muestreo
//        Gizmos.color = Color.green;
//        Gizmos.DrawWireCube(transform.position, Vector3.one * cubeSize);

//        // Dibujar los puntos de la nube de puntos
//        Gizmos.color = Color.red;
//        foreach (Vector3 point in pointCloud)
//        {
//            Gizmos.DrawSphere(point, 0.1f);
//        }
//    }
//}