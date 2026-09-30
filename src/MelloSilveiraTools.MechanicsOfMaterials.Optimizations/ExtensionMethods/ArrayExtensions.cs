namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.ExtensionMethods;

public static class ArrayExtensions
{
    public static double[] Normalize(this double[] array, double? value = null)
    {
        double[] normalizedArray = new double[array.Length];

        double max = value ?? array.Max();
        for (int i = 0; i < array.Length; i++)
        {
            normalizedArray[i] = array[i] / max;
        }

        return normalizedArray;
    }

    public static double[] TranslateToOrigin(this double[] array, double? value = null)
    {
        double[] distanceToOrigin = new double[array.Length];

        double b = value ?? array[0];
        for (int i = 0; i < array.Length; i++)
        {
            distanceToOrigin[i] = array[i] - b;
        }

        return distanceToOrigin;
    }
}
