using NUnit.Framework;
using Spotlight.Domain.Story;

namespace Spotlight.Tests.EditMode
{
    public sealed class StoryTypewriterTests
    {
        [Test]
        public void Reveal_ShowsOnlyWholeCharactersUntilComplete()
        {
            StoryTypewriter typewriter = new StoryTypewriter();
            typewriter.Restart("感谢你");

            typewriter.Reveal(0.01f, 30f);
            Assert.That(typewriter.VisibleCount, Is.EqualTo(0));
            Assert.That(typewriter.IsComplete, Is.False);

            typewriter.Reveal(0.04f, 30f);
            Assert.That(typewriter.VisibleText, Is.EqualTo("感"));
            Assert.That(typewriter.Confirm(), Is.False);
            Assert.That(typewriter.VisibleText, Is.EqualTo("感谢你"));
            Assert.That(typewriter.Confirm(), Is.True);
        }

        [Test]
        public void Reveal_DoesNotAdvanceWhenTimeOrSpeedIsNotPositive()
        {
            StoryTypewriter typewriter = new StoryTypewriter();
            typewriter.Restart("旁白");

            typewriter.Reveal(0f, 30f);
            typewriter.Reveal(1f, 0f);
            typewriter.Reveal(-1f, 30f);

            Assert.That(typewriter.VisibleCount, Is.EqualTo(0));
            Assert.That(typewriter.Confirm(), Is.False);
            Assert.That(typewriter.IsComplete, Is.True);
        }

        [Test]
        public void Restart_EmptyTextCanAdvanceImmediately()
        {
            StoryTypewriter typewriter = new StoryTypewriter();
            typewriter.Restart(null);

            Assert.That(typewriter.IsComplete, Is.True);
            Assert.That(typewriter.Confirm(), Is.True);
        }
    }
}
