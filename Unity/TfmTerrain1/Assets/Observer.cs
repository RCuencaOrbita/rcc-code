using UnityEngine;
using System.Collections.Generic;
using System;
using System.IO;
using System.Linq;

public class Observer : MonoBehaviour
{
    public GameObject vehicle;
    public LayerMask terrainLayer;
    public float lidarDistance = 10f;
    public int rayCount = 100;
    public float cubeSize = 3f;
    public float horizontalStep = 0.5f; // Ajusta este valor para cambiar la resolución horizontal
    public float verticalStep = 0.5f;  // Ajusta este valor para cambiar la resolución vertical
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
            // Obtener información sobre el contacto
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
        // Calcular la posición del LiDAR
        Vector3 lidarPosition = vehicle.transform.position + vehicle.transform.forward * -lidarDistance;
        lidarPosition.y = lidarPosition.y + 10;

        // Punto de inicio del barrido
        Vector3 startPoint = lidarPosition + vehicle.transform.right * -horizontalRange / 2 + vehicle.transform.up * verticalRange / 2;
        Vector3 vehiclePosition = vehicle.transform.position;
        // Definir los ejes locales del cubo de observación:
        // - forward sigue siendo la dirección del vehículo
        // - right está en el plano horizontal, perpendicular al forward
        // - up es el eje global 'up' (Vector3.up)
        Vector3 vehicleForward = vehicle.transform.forward;
        vehicleForward.y = 0; // Hacer que el 'forward' sea horizontal
        vehicleForward.Normalize();
        Vector3 vehicleRight = Vector3.Cross(Vector3.up, vehicleForward); // Right perpendicular a forward y up

        // proyectar los puntos de contacto en el eje de coordenadas del cubo
        for (int i = 0; i < contacts.Count; i++)
        {
            if (contacts[i] == Vector3.zero)
            {
                continue;
            }
            contacts[i] = contacts[i] - vehiclePosition;
            // Proyectar el punto de impacto en los ejes locales modificados
            float forwardProjection = Vector3.Dot(contacts[i], vehicleForward);
            float rightProjection = Vector3.Dot(contacts[i], vehicleRight);
            float upProjection = Vector3.Dot(contacts[i], Vector3.up); // Usa el eje global 'up'
            contacts[i] = new Vector3(forwardProjection, upProjection, rightProjection);
        }




        // Iterar sobre cada punto del barrido       
        for (float y = verticalRange; y >= -verticalRange; y -= verticalStep)
        {
            for (float x = -horizontalRange; x <= horizontalRange; x += horizontalStep)
            {
                // Componer la dirección inicial para los raycast
                Vector3 direction = vehicle.transform.forward + vehicle.transform.right * ((-horizontalRange / 2) + x) + vehicle.transform.up * y;

                // Normalizar la dirección para asegurar que la magnitud sea 1
                direction.Normalize();

                Ray ray = new Ray(lidarPosition, direction * 100);
                //PARA VER LOS RAYOS: Debug.DrawRay(lidarPosition, direction * 100, Color.red, 5);

                if (Physics.Raycast(ray, out RaycastHit hit, 200, terrainLayer))
                {
                    // Obtener la posición del impacto 
                    Vector3 hitPoint = hit.point;


                    // Calcular el vector de diferencia entre el punto de impacto y el vehículo
                    Vector3 localHitPoint = hitPoint - vehiclePosition;

                    // Proyectar el punto de impacto en los ejes locales modificados
                    float forwardProjection = Vector3.Dot(localHitPoint, vehicleForward);
                    float rightProjection = Vector3.Dot(localHitPoint, vehicleRight);
                    float upProjection = Vector3.Dot(localHitPoint, Vector3.up); // Usa el eje global 'up'

                    // Comprobar si el punto proyectado está dentro del cubo
                    if (Mathf.Abs(forwardProjection) <= cubeSize / 2 &&
                        Mathf.Abs(rightProjection) <= cubeSize / 2 &&
                        Mathf.Abs(upProjection) <= cubeSize / 2)
                    {
                        // particles.Add(hitPoint);
                        particles.Add(new Vector3(forwardProjection, upProjection, rightProjection));
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
