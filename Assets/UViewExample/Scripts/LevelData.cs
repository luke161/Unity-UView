using UnityEngine;
using System.Collections;

[System.Serializable]
public class LevelData 
{

	public string levelName;
	public Color backgroundColor;

	public LevelData(string levelName, Color backgroundColor)
	{
		this.levelName = levelName;
		this.backgroundColor = backgroundColor;
	}

}

