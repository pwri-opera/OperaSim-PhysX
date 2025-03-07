using UnityEngine;

class Utils {
    public static string PreprocessNamespace(GameObject obj, string topicName) {
        var robotName = obj.transform.root.name;
        return topicName.Replace("[robot_name]", robotName);
    }
}
