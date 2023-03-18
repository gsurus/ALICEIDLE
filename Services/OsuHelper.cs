using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;

namespace ALICEIDLE.Services
{
    public class OsuHelper
    {
        public static async Task GetOsuFilesData(string path = @"E:\Games\osu!\Songs" )
        {
            string[] files = await GetOsuBeatmaps(path);
            List<OsuFile> osuFiles = new List<OsuFile>();

            foreach (string file in files)
                osuFiles.Add(await DeserializeOsuFile(file));


        }
        
        public static async Task<BeatmapStats> GetBeatmapStatistics(OsuFile file)
        {
            BeatmapStats stats = new BeatmapStats();
            double bpm = 1 / file.timingPoints.First().beatLength * 1000 * 60;
            TimeSpan timeSpan = TimeSpan.FromMilliseconds(file.hitObjects.Last().Time);
            string length = string.Format("{0}:{1:D2}", (int)timeSpan.TotalMinutes, timeSpan.Seconds);
            double lengthSeconds = file.hitObjects.Last().Time / 1000;
            int hitCircles = 0;
            int sliders = 0;

            foreach (var obj in file.hitObjects)
            {
                if (!obj.IsSlider)
                    hitCircles++;
                else
                    sliders++;
            }

            stats.bpm = bpm;
            stats.length = length;
            stats.hitCircles = hitCircles;
            stats.sliders = sliders;
            stats.hitCirclesPerSecond = hitCircles / lengthSeconds;
            stats.slidersPerSecond = sliders / lengthSeconds;

            //Console.WriteLine($"BPM: {bpm}\nLength: {length}\nHit Circles: {hitCircles}\nSliders: {sliders}");
            var probability = CalculateStreamLikelihood(stats.hitCirclesPerSecond, stats.slidersPerSecond);
            //Console.WriteLine($"Stream Prob: {probability}");
            return stats;
        }
        
        public static double CalculateStreamLikelihood(double hitCirclesPerSecond, double slidersPerSecond)
        {
            // Calculate the total number of objects per second
            double objectsPerSecond = hitCirclesPerSecond + slidersPerSecond;

            // Calculate the ratio of sliders to total objects
            double sliderRatio = slidersPerSecond / objectsPerSecond;

            // Calculate the likelihood based on the slider ratio
            double likelihood = Math.Pow(sliderRatio, 2) * 100;

            return likelihood;
        }

        public static async Task<OsuFile> DeserializeOsuFile(string filePath)
        {

            Dictionary<string, string[]> osuFileDict = await ParseOsuFile(File.ReadAllLines(filePath));
            
            OsuFile osuFile = new OsuFile();
            osuFile.metadata = new Metadata();
            osuFile.hitObjects = new List<HitObject>();
            osuFile.timingPoints = new List<TimingPoint>();
            osuFile.difficulty = new Difficulty();

            Difficulty diff = osuFile.difficulty;
            Metadata meta = osuFile.metadata;
            List<TimingPoint> timingPts = osuFile.timingPoints;
            List<HitObject> hitObjs = osuFile.hitObjects;

            foreach (var data in osuFileDict["metadata"])
            {
                if(data.StartsWith("Title"))
                    meta.title = data.Split(':')[1].Trim();
                if (data.StartsWith("Artist"))
                    meta.artist = data.Split(':')[1].Trim();
                if (data.StartsWith("Creator"))
                    meta.creator = data.Split(':')[1].Trim();
                if (data.StartsWith("Version"))
                    meta.version = data.Split(':')[1].Trim();
                if (data.StartsWith("Source"))
                    meta.source = data.Split(':')[1].Trim();
                if (data.StartsWith("Tags"))
                    meta.tags = data.Split(':')[1].Trim();
                if (data.StartsWith("BeatmapID"))
                    meta.beatmapId = Int32.Parse(data.Split(':')[1].Trim());
                if (data.StartsWith("BeatmapSetID"))
                    meta.beatmapSetId = Int32.Parse(data.Split(':')[1].Trim());
            }

            foreach (var data in osuFileDict["diff"])
            {
                if(data.StartsWith("HpDrainRate"))
                    diff.HPDrainRate = double.Parse(data.Split(':')[1].Trim());
                if(data.StartsWith("CircleSize"))
                    diff.CircleSize = double.Parse(data.Split(":")[1].Trim());
                if(data.StartsWith("OverallDifficulty"))
                    diff.OverallDifficulty = double.Parse(data.Split(":")[1].Trim());
                if (data.StartsWith("ApproachRate"))
                    diff.ApproachRate = double.Parse(data.Split(":")[1].Trim());
                if (data.StartsWith("SliderMultiplier"))
                    diff.SliderMultiplier = double.Parse(data.Split(":")[1].Trim());
                if (data.StartsWith("SliderTickRate"))
                    diff.SliderTickRate = double.Parse(data.Split(":")[1].Trim());
            }

            foreach (var data in osuFileDict["timing"])
            {
                if (data.Count() <= 0)
                    continue;

                var timingValues = data.Split(',');
                TimingPoint point = new TimingPoint();
                point.time = Int32.Parse(timingValues[0]);
                point.beatLength = Double.Parse(timingValues[1]);
                point.meter = Int32.Parse(timingValues[2]);
                point.sampleSet = Int32.Parse(timingValues[3]);
                point.sampleIndex = Int32.Parse(timingValues[4]);
                point.volume = Int32.Parse(timingValues[5]);
                point.uninherited = timingValues[6] == "1" ? true : false;
                point.effects = Int32.Parse(timingValues[7]);

                timingPts.Add(point);
            }

            foreach (var data in osuFileDict["hit"])
            {
                HitObject obj = new HitObject();

                if (data.Contains('|'))
                    obj.IsSlider = true;

                var hitVals = data.Split(',');

                obj.X = Int32.Parse(hitVals[0]);
                obj.Y = Int32.Parse(hitVals[1]);
                obj.Time = Int32.Parse(hitVals[2]);
                obj.Type = Int32.Parse(hitVals[3]);

                hitObjs.Add(obj);
            }

            //WriteInfoToConsole(osuFile);
            
            return osuFile;
        }

        public static async Task<Dictionary<string, string[]>> ParseOsuFile(string[] lines)
        {
            Dictionary<string, string[]> fileData = new Dictionary<string, string[]>();
            
            int timingPtsStartIndex = Array.IndexOf(lines, "[TimingPoints]") + 1;
            int timingPtsEndIndex = Array.FindIndex(lines, timingPtsStartIndex, line => line == "");

            string[] timingPtsLines = lines
                .Skip(timingPtsStartIndex)
                .Take(timingPtsEndIndex - timingPtsStartIndex + 1)
                .ToArray();

            int hitObjStartIndex = Array.IndexOf(lines, "[HitObjects]") + 1;
            int hitObjEndIndex = lines.Count() - 1;
            string[] hitObjLines = lines
                .Skip(hitObjStartIndex)
                .Take(hitObjEndIndex - hitObjStartIndex + 1)
                .ToArray();

            int metadataStartIndex = Array.IndexOf(lines, "[Metadata]") + 1;
            int metadataEndIndex = Array.FindIndex(lines, metadataStartIndex, line => line == "");

            string[] metadataLines = lines
                .Skip(metadataStartIndex)
                .Take(metadataEndIndex - metadataStartIndex + 1)
                .ToArray();

            int diffStartIndex = Array.IndexOf(lines, "[Difficulty]") + 1;
            int diffEndIndex = Array.FindIndex(lines, diffStartIndex, line => line == "");

            string[] diffLines = lines
                .Skip(diffStartIndex)
                .Take(diffEndIndex - diffStartIndex + 1)
                .ToArray();

            fileData.Add("timing", timingPtsLines);
            fileData.Add("hit", hitObjLines);
            fileData.Add("metadata", metadataLines);
            fileData.Add("diff", diffLines);
            Console.WriteLine(hitObjLines.Count());
            return fileData;
        }
        public static async Task<string[]> GetOsuBeatmaps(string path)
        {
            string searchPattern = "*.osu";

            // SearchOption.AllDirectories will include all subdirectories
            string[] files = Directory.GetFiles(path, searchPattern, SearchOption.AllDirectories);

            // Output the list of files found
            foreach (string file in files)
            {
                Console.WriteLine(file);
            }

            return files;
        }

        public static void WriteInfoToConsole(OsuFile osuFile)
        {
            foreach (var property in osuFile.GetType().GetProperties())
            {
                if (property.Name == "timingPoints")
                    Console.WriteLine($"timingPoints: {string.Join("; ", ((List<TimingPoint>)property.GetValue(osuFile)).Select(tp => tp.ToString()))}");
                else if (property.Name == "hitObjects")
                    Console.WriteLine($"hitObjects: {string.Join("; ", ((List<HitObject>)property.GetValue(osuFile)).Select(ho => ho.ToString()))}");
                else if (property.Name == "metadata")
                    Console.WriteLine($"metadata: {property.GetValue(osuFile).ToString()}");
                else
                    Console.WriteLine($"{property.Name}: {property.GetValue(osuFile)}");
            }
        }
    }
    public class BeatmapStats
    {
        public int hitCircles { get; set; }
        public int sliders { get; set; }
        public double hitCirclesPerSecond { get; set; }
        public double slidersPerSecond { get; set; }
        public double bpm { get; set; }
        public string length { get; set; }
    }
    public class OsuFile
    {
        public Metadata metadata { get; set; }
        public List<TimingPoint> timingPoints { get; set; }
        public List<HitObject> hitObjects { get; set; }
        public Difficulty difficulty { get; set; }

    }
    public class Metadata
    {
        public string title { get; set; }
        public string artist { get; set; }
        public string creator { get; set; }
        public string version { get; set; }
        public string source { get; set; }
        public string tags { get; set; }
        public int  beatmapId { get; set; }
        public int beatmapSetId { get; set; }
        public override string ToString()
        {
            return $"title: {title}, artist: {artist}, creator: {creator}, version: {version}, source: {source}, tags: {tags}, beatmapId: {beatmapId}, beatmapSetId: {beatmapSetId}";
        }
    }
    public class TimingPoint
    {
        public int time { get; set; }
        public double beatLength { get; set; }
        public int meter { get; set; }
        public int sampleSet { get; set; }
        public int sampleIndex { get; set; }
        public int volume { get; set; }
        public bool uninherited { get; set; }
        public int effects { get; set; }
        public override string ToString()
        {
            return $"time: {time}, beatLength: {beatLength}, meter: {meter}, sampleSet: {sampleSet}, sampleIndex: {sampleIndex}, volume: {volume}, uninherited: {uninherited}, effects: {effects}";
        }
    }
    public class HitObject
    {
        public int X { get; set; }
        public int Y { get; set; }
        public int Time { get; set; }
        public int Type { get; set; }
        public int HitSound { get; set; }
        public List<int> ObjectParams { get; set; }
        public List<int> HitSample { get; set; }
        public bool IsSlider { get; set; }
        public override string ToString()
        {
            return $"X: {X}, Y: {Y}, Time: {Time}, Type: {Type}, HitSound: {HitSound}, IsSlider: {IsSlider}, ObjectParams: {string.Join(",", ObjectParams)}, HitSample: {string.Join(",", HitSample)}";
        }
    }
    public class Difficulty
    {
        public double HPDrainRate { get; set; }
        public double CircleSize { get; set; }
        public double OverallDifficulty { get; set; }
        public double ApproachRate { get; set; }
        public double SliderMultiplier { get; set; }
        public double SliderTickRate { get; set; }
    }
}
