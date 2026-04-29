using UnityEngine;

// Скрипт для хранения данных, необходимых для отображения надписи при взгляде
public class GazeTargetData : MonoBehaviour
{
    [TextArea]
    [Tooltip("Текст, который будет отображаться при взгляде на объект")]
    public string displayText = "Default Label";
}