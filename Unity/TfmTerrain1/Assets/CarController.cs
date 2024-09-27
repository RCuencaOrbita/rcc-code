//using UnityEngine;

//public class CarController : MonoBehaviour
//{
//    public WheelCollider[] wheelColliders;
//    public float motorTorque = 100;

//    void FixedUpdate()
//    {
//        foreach (WheelCollider wheel in wheelColliders)
//        {
//            wheel.motorTorque = motorTorque;
//        }
//    }
//}
using UnityEngine;

public class CarController : MonoBehaviour
{
    public float velocidadMaxima = 10f;
    public float fuerzaMotor = 50f;
    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    void FixedUpdate()
    {
        // Obtener la velocidad actual (magnitud del vector de velocidad)
        float velocidadActual = rb.velocity.magnitude;

        // Si la velocidad actual es menor a la velocidad máxima
        if (velocidadActual < velocidadMaxima)
        {
            // Calcular la fuerza a aplicar (ejemplo de función lineal)
            float fuerzaAAplicar = (velocidadMaxima - velocidadActual) * fuerzaMotor;

            // Aplicar la fuerza al vehículo (en la dirección de movimiento)
            rb.AddForce(transform.forward * fuerzaAAplicar, ForceMode.Acceleration);
        }
    }
}