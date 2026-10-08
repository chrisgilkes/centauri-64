using System;
using System.IO;

using Centauri64.Basic;
using Centauri64.Console;
using Centauri64.Machine;
using Centauri64.Machine.Images;
using Centauri64.Machine.Sprites;
using Centauri64.Network;
using Centauri64.Session;

namespace Centauri64.Analysis;

/// <summary>
/// Issue #3 Image → Sprite bridge checks.
/// Run with: Centauri64.exe --verify-sprite-pipeline
/// </summary>
public static class SpritePipelineVerification
{
    public static int Run()
    {
        var failed = 0;
        System.Console.WriteLine("SPRITE PIPELINE VERIFICATION");
        System.Console.WriteLine();

        failed += RestrictedArtworkAccessWithSprites();
        failed += FullImageEditorLockedWithoutImages();
        failed += HardcoreUnrestricted();
        failed += CreateEligibleSpriteArtwork();
        failed += RejectNonSixteenArtwork();
        failed += BindMultipleFramesPreservesTransparency();
        failed += LegacySpritesFileLoads();
        failed += NewSpriteSaveLoadRoundTrip();
        failed += BasicSpriteAndAnimCommands();

        FeatureGate.Current = FeatureAvailability.AllReleased();

        System.Console.WriteLine();
        System.Console.WriteLine(
            failed == 0 ? "ALL SPRITE PIPELINE CHECKS PASSED" : failed + " FAILED");
        return failed;
    }

    private static int RestrictedArtworkAccessWithSprites()
    {
        FeatureGate.Current = FeatureAvailability.FromUnlocks(new[]
        {
            FeatureId.Sprites
        });

        var ok =
            FeatureGate.CanOpenSpriteArtworkEditor() &&
            !FeatureGate.CanOpenFullImageEditor() &&
            FeatureGate.Current.IsAvailable(FeatureId.Sprites) &&
            !FeatureGate.Current.IsAvailable(FeatureId.Images);

        return Expect("Issue #3 Sprites unlocks Sprite Artwork (not full Images)", ok);
    }

    private static int FullImageEditorLockedWithoutImages()
    {
        FeatureGate.Current = FeatureAvailability.FromUnlocks(new[]
        {
            FeatureId.Graphics
        });

        var ok =
            !FeatureGate.CanOpenSpriteArtworkEditor() &&
            !FeatureGate.CanOpenFullImageEditor() &&
            !FeatureGate.Current.IsAvailable(FeatureId.Sprites);

        return Expect("Pre-Issue-3 cannot open artwork or sprites", ok);
    }

    private static int HardcoreUnrestricted()
    {
        FeatureGate.Current = FeatureAvailability.AllReleased();

        var ok =
            FeatureGate.CanOpenSpriteArtworkEditor() &&
            FeatureGate.CanOpenFullImageEditor() &&
            FeatureGate.Current.IsAvailable(FeatureId.Sprites) &&
            FeatureGate.Current.IsAvailable(FeatureId.Images);

        return Expect("Hardcore unrestricted sprite + image tools", ok);
    }

    private static int CreateEligibleSpriteArtwork()
    {
        FeatureGate.Current = FeatureAvailability.AllReleased();
        var images = new ImageAssetStore();
        var editor = new ImageEditor(images);
        editor.Open(ImageEditor.AccessMode.SpriteArtwork);

        var ghost = new ImageAsset(
            "GHOST",
            CentauriDisplayMode.HighResolution,
            16,
            16,
            ImageCategory.Sprite);
        ghost.SetPixel(8, 8, 7);
        ghost.AddFrame();
        ghost.GetFrame(1).Pixels[8, 8] = 15;
        images.Add(ghost);

        var visible = SpriteArtwork.IsEligible(ghost) &&
                      images.Count == 1 &&
                      editor.IsSpriteArtworkMode;

        // Non-eligible assets must not appear in restricted browsing via store filter rules.
        var big = new ImageAsset(
            "BIG",
            CentauriDisplayMode.HighResolution,
            32,
            32,
            ImageCategory.Sprite);
        images.Add(big);

        var general = new ImageAsset(
            "PIC",
            CentauriDisplayMode.HighResolution,
            16,
            16,
            ImageCategory.General);
        images.Add(general);

        return Expect(
            "create eligible 16x16 Sprite artwork",
            visible &&
            SpriteArtwork.IsEligible(ghost) &&
            !SpriteArtwork.IsEligible(big) &&
            !SpriteArtwork.IsEligible(general));
    }

    private static int RejectNonSixteenArtwork()
    {
        var anim = new SpriteAnimation("DEFAULT");
        anim.AddFrame();

        var bad = new ImageAsset(
            "WIDE",
            CentauriDisplayMode.HighResolution,
            32,
            32,
            ImageCategory.Sprite);

        try
        {
            SpriteArtwork.BakeIntoAnimation(anim, bad);
            return Expect("reject non-16x16 for hardware sprites", false);
        }
        catch (InvalidOperationException ex)
        {
            return Expect(
                "reject non-16x16 for hardware sprites",
                ex.Message.Contains("16X16", StringComparison.OrdinalIgnoreCase));
        }
    }

    private static int BindMultipleFramesPreservesTransparency()
    {
        var image = new ImageAsset(
            "GHOST",
            CentauriDisplayMode.HighResolution,
            16,
            16,
            ImageCategory.Sprite);
        image.SetPixel(1, 1, 7);
        image.AddFrame();
        image.SelectFrame(1);
        image.SetPixel(2, 2, 15);

        var asset = new SpriteAsset("GHOST");
        var anim = asset.AddAnimation("DEFAULT");
        anim.AddFrame();

        SpriteArtwork.BakeIntoAnimation(anim, image);

        var f0 = anim.Frames[0];
        var f1 = anim.Frames[1];

        var ok =
            anim.Frames.Count == 2 &&
            anim.SourceImageName == "GHOST" &&
            f0.Pixels[1, 1] == 7 &&
            f0.Pixels[0, 0] == CentauriSprite.TRANSPARENT &&
            f1.Pixels[2, 2] == 15 &&
            f1.Pixels[0, 0] == CentauriSprite.TRANSPARENT;

        return Expect("bind multiple image frames + transparency", ok);
    }

    private static int LegacySpritesFileLoads()
    {
        FeatureGate.Current = FeatureAvailability.AllReleased();
        var path = FindListing("HAG.sprites");
        if (path == null)
            return Expect("legacy HAG.sprites present", false);

        var dir = Path.GetDirectoryName(path)!;
        var name = Path.GetFileNameWithoutExtension(path);
        var store = new SpriteAssetStore();

        // SpriteStorage always reads from TapeFolder — copy into a temp tape name via API.
        // Instead, parse via temporary file in TapeFolder.
        TapeFolder.EnsureExists();
        var tapeName = "_VERIFY_HAG";
        var dest = Path.Combine(TapeFolder.Location, tapeName + ".sprites");
        File.Copy(path, dest, overwrite: true);

        try
        {
            var storage = new SpriteStorage();
            storage.Load(tapeName, store);

            var ok = store.Contains("HAG");
            if (ok)
            {
                var hag = store.Get("HAG");
                var frame = hag.GetAnimation("DEFAULT").Frames[0];
                // HAG row 0: indices 6,7 are colour 15; others transparent.
                ok = hag.ContainsAnimation("DEFAULT") &&
                     frame.Pixels[0, 0] == CentauriSprite.TRANSPARENT &&
                     frame.Pixels[0, 6] == 15;
            }

            return Expect("existing .sprites compatibility (HAG)", ok);
        }
        finally
        {
            if (File.Exists(dest))
                File.Delete(dest);
        }
    }

    private static int NewSpriteSaveLoadRoundTrip()
    {
        FeatureGate.Current = FeatureAvailability.AllReleased();
        TapeFolder.EnsureExists();
        var tapeName = "_VERIFY_GHOST";
        var spritesPath = Path.Combine(TapeFolder.Location, tapeName + ".sprites");
        var imagesPath = Path.Combine(TapeFolder.Location, tapeName + ".images");

        try
        {
            var images = new ImageAssetStore();
            var image = new ImageAsset(
                "GHOST",
                CentauriDisplayMode.HighResolution,
                16,
                16,
                ImageCategory.Sprite);
            image.SetPixel(4, 4, 5);
            image.AddFrame();
            image.GetFrame(1).Pixels[5, 5] = 9;
            images.Add(image);

            var sprites = new SpriteAssetStore();
            var asset = new SpriteAsset("GHOST");
            var anim = asset.AddAnimation("DEFAULT");
            SpriteArtwork.BakeIntoAnimation(anim, image);
            sprites.Add(asset);

            new SpriteStorage().Save(tapeName, sprites);
            new ImageStorage().Save(tapeName, images);

            var loadedSprites = new SpriteAssetStore();
            new SpriteStorage().Load(tapeName, loadedSprites);

            // Playback must work from baked pixels even if .images deleted.
            File.Delete(imagesPath);

            var ok = loadedSprites.Contains("GHOST");
            if (ok)
            {
                var loadedAnim = loadedSprites.Get("GHOST").GetAnimation("DEFAULT");
                ok = loadedAnim.Frames.Count == 2 &&
                     loadedAnim.SourceImageName == "GHOST" &&
                     loadedAnim.Frames[0].Pixels[4, 4] == 5 &&
                     loadedAnim.Frames[1].Pixels[5, 5] == 9 &&
                     loadedAnim.Frames[0].Pixels[0, 0] == CentauriSprite.TRANSPARENT;
            }

            return Expect("new sprite SAVE/LOAD round-trip (baked pixels)", ok);
        }
        finally
        {
            if (File.Exists(spritesPath))
                File.Delete(spritesPath);
            if (File.Exists(imagesPath))
                File.Delete(imagesPath);
        }
    }

    private static int BasicSpriteAndAnimCommands()
    {
        FeatureGate.Current = FeatureAvailability.AllReleased();

        var console = new TextConsole();
        var programConsole = new TextConsole();
        var machine = new CentauriMachine(console, programConsole);
        var network = new NetworkService();
        var interpreter = new Interpreter(console, machine, network);
        var program = new BasicProgram();
        var tokenizer = new Tokenizer();
        var parser = new Parser();

        var image = new ImageAsset(
            "GHOST",
            CentauriDisplayMode.HighResolution,
            16,
            16,
            ImageCategory.Sprite);
        image.SetPixel(3, 3, 7);
        image.AddFrame();
        image.GetFrame(1).Pixels[3, 3] = 8;
        machine.ImageAssets.Add(image);

        var asset = new SpriteAsset("GHOST");
        var anim = asset.AddAnimation("DEFAULT");
        SpriteArtwork.BakeIntoAnimation(anim, image);
        var walk = asset.AddAnimation("WALK");
        SpriteArtwork.BakeIntoAnimation(walk, image);
        machine.SpriteAssets.Add(asset);

        void Store(string line) =>
            program.StoreLine(parser.ParseLine(tokenizer.Tokenize(line), line));

        Store("10 MODE 2");
        Store("20 SPRITE 0,\"GHOST\"");
        Store("30 SPRITEPOS 0,40,40");
        Store("40 SPRITEANIM 0,\"WALK\",1");
        Store("50 YIELD");
        Store("60 END");

        interpreter.Start(program);

        for (var i = 0; i < 700 && interpreter.IsRunning; i++)
        {
            var action = interpreter.ExecuteNextInstruction();
            if (action == ExecutionAction.Yield ||
                action == ExecutionAction.Wait ||
                action == ExecutionAction.Input)
            {
                break;
            }
        }

        var sprite = machine.Sprites[0];
        var ok =
            sprite.Visible &&
            sprite.AssetName == "GHOST" &&
            sprite.AnimationName == "WALK" &&
            sprite.AnimationLoop &&
            sprite.Pixels[3, 3] == 7;

        return Expect("BASIC SPRITE + SPRITEANIM behaviour", ok);
    }

    private static string? FindListing(string name)
    {
        var candidates = new[]
        {
            Path.Combine(AppContext.BaseDirectory, "Listings", name),
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "Listings", name),
            Path.Combine(Directory.GetCurrentDirectory(), "Centauri64", "Listings", name),
            Path.Combine(Directory.GetCurrentDirectory(), "Listings", name)
        };

        foreach (var path in candidates)
        {
            if (File.Exists(path))
                return Path.GetFullPath(path);
        }

        return null;
    }

    private static int Expect(string label, bool condition)
    {
        System.Console.WriteLine((condition ? "PASS " : "FAIL ") + label);
        return condition ? 0 : 1;
    }
}
