using UnityEngine;
using UnityEngine.Rendering;

public class ControleTimeScale : MonoBehaviour
{

	#region Fields

	// Variables publiques pour contrôler le temps et le taux de rafraîchissement
	[Range(1, 100)]
	public int divideTime = 1;

	[Range(1, 100)]
	public int multiplyTime = 1;

	[Range(0, 100)]
	public int targetFrameRate = 30;

	public bool useRenderFrameInterval = true;

	[Range(1, 100)]
	public int renderFrame = 1;

	#endregion

	#region Init

	private void Awake()
	{
		// Désactive la synchronisation verticale
		QualitySettings.vSyncCount = 0;
	}

	#endregion

	#region Update

	private void Update()
	{
		// Définit le taux de rafraîchissement cible de l'application
		Application.targetFrameRate = targetFrameRate;

		// Contrôle de l'intervalle de rendu des frames
		if (useRenderFrameInterval)
		{
			if (targetFrameRate > 0)
			{
				// Calcule l'intervalle de rendu en fonction du taux de rafraîchissement cible et du rendu des frames
				OnDemandRendering.renderFrameInterval = targetFrameRate / renderFrame;
			}
			else
			{
				// Si le taux de rafraîchissement cible est 0, utilise une valeur par défaut de 120
				OnDemandRendering.renderFrameInterval = 120 / renderFrame;
			}
		}
		else
		{
			// Si useRenderFrameInterval est false, l'intervalle de rendu est fixé à 1
			OnDemandRendering.renderFrameInterval = 1;
		}

		// Contrôle de l'échelle de temps
		if (divideTime > 1)
		{
			// Divise l'échelle de temps par divideTime et réinitialise multiplyTime à 1
			Time.timeScale = 1f / divideTime;
			multiplyTime = 1;
		}
		else
		{
			// Multiplie l'échelle de temps par multiplyTime
			Time.timeScale = multiplyTime;
		}
	}

	#endregion

}
