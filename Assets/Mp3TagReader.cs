using System;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

public class Mp3TagReader : MonoBehaviour
{
    [Header("Mp3 File Path (must end in .mp3)")]
    public string filePath; // Path to the MP3 file

    [Header("Scene References")]
    public Text songTitleText; // UI Text for song title
    public Text songArtistText; // UI Text for artist
    public Text songAlbumText; // UI Text for album

    void Start()
    {
        // Start reading ID3 tags
        ReadID3Tags(filePath);
    }

    void ReadID3Tags(string path)
    {
        if (!File.Exists(path))
        {
            Debug.LogError("File not found: " + path);
            return;
        }

        using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read))
        {
            // Check for ID3v2 tags
            fs.Seek(0, SeekOrigin.Begin);
            byte[] header = new byte[10];
            fs.Read(header, 0, 10);

            if (Encoding.ASCII.GetString(header, 0, 3) == "ID3")
            {
                // Calculate the size of the ID3v2 tag
                int tagSize = (header[6] & 0x7F) << 21 | (header[7] & 0x7F) << 14 | (header[8] & 0x7F) << 7 | (header[9] & 0x7F);
                byte[] tagData = new byte[tagSize];
                fs.Read(tagData, 0, tagSize);

                // Extract and process ID3v2 frames
                ExtractID3v2Tags(tagData);
                return;
            }

            Debug.Log("No ID3v2 tags found.");
        }
    }

    void ExtractID3v2Tags(byte[] tagData)
    {
        int index = 0;

        // Loop through all frames in the ID3v2 tag
        while (index < tagData.Length - 10)
        {
            string frameId = Encoding.ASCII.GetString(tagData, index, 4).TrimEnd('\0');
            int frameSize = (tagData[index + 4] & 0x7F) << 21 |
                            (tagData[index + 5] & 0x7F) << 14 |
                            (tagData[index + 6] & 0x7F) << 7 |
                            (tagData[index + 7] & 0x7F);

            // Validate frame size
            if (frameSize <= 0 || index + 10 + frameSize > tagData.Length)
                break;

            // Extract frame content, accounting for the encoding byte
            byte encodingByte = tagData[index + 10];
            Encoding encoding = encodingByte switch
            {
                0 => Encoding.GetEncoding("ISO-8859-1"), // ISO-8859-1
                1 => Encoding.Unicode,                   // UTF-16 with BOM
                2 => Encoding.BigEndianUnicode,          // UTF-16BE
                3 => Encoding.UTF8,                      // UTF-8
                _ => Encoding.UTF8,                      // Default to UTF-8
            };

            // Extract frame content, starting after the encoding byte
            string frameContent = encoding.GetString(tagData, index + 11, frameSize - 1).TrimEnd('\0');

            // Handle specific frame IDs
            switch (frameId)
            {
                case "TIT2": // Title
                    Debug.Log("Title: " + frameContent);
                    songTitleText.text = frameContent;
                    break;
                case "TPE1": // Artist
                    Debug.Log("Artist: " + frameContent);
                    songArtistText.text = "by " + frameContent;
                    break;
                case "TALB": // Album
                    Debug.Log("Album: " + frameContent);
                    songAlbumText.text = "on " + frameContent;
                    break;
            }

            // Move to the next frame
            index += 10 + frameSize;
        }
    }
}
