using Microsoft.VisualStudio.TestTools.UnitTesting;
using Skender.Stock.Indicators;

namespace Internal.Tests;

[TestClass]
public class HeikinAshi : TestBase
{
    [TestMethod]
    public void Standard()
    {
        List<HeikinAshiResult> results = quotes.GetHeikinAshi().ToList();

        // assertions

        // should always be the same number of results as there is quotes
        Assert.AreEqual(502, results.Count);

        // sample value
        HeikinAshiResult r = results[501];
        Assert.AreEqual(241.3018m, NullMath.Round(r.Open, 4));
        Assert.AreEqual(245.54m, NullMath.Round(r.High, 4));
        Assert.AreEqual(241.3018m, NullMath.Round(r.Low, 4));
        Assert.AreEqual(244.6525m, NullMath.Round(r.Close, 4));
        Assert.AreEqual(147031456m, r.Volume);
    }

    [TestMethod]
    public void UseAsQuotes()
    {
        IEnumerable<HeikinAshiResult> haQuotes = quotes.GetHeikinAshi();
        IEnumerable<SmaResult> haSma = haQuotes.GetSma(5);
        Assert.AreEqual(498, haSma.Count(x => x.Sma != null));
    }

    [TestMethod]
    public void ToQuotes()
    {
        List<HeikinAshiResult> results = quotes.GetHeikinAshi().ToList();
        List<Quote> haQuotes = results.ToQuotes();

        for (int i = 0; i < results.Count; i++)
        {
            HeikinAshiResult r = results[i];
            Quote q = haQuotes[i];

            Assert.AreEqual(r.Date, q.Date);
            Assert.AreEqual(r.Open, q.Open);
            Assert.AreEqual(r.High, q.High);
            Assert.AreEqual(r.Low, q.Low);
            Assert.AreEqual(r.Close, q.Close);
            Assert.AreEqual(r.Volume, q.Volume);
        }
    }

    [TestMethod]
    public void BadData()
    {
        IEnumerable<HeikinAshiResult> r = Indicator.GetHeikinAshi(badQuotes);
        Assert.AreEqual(502, r.Count());
    }

    [TestMethod]
    public void NoQuotes()
    {
        IEnumerable<HeikinAshiResult> r0 = noquotes.GetHeikinAshi();
        Assert.AreEqual(0, r0.Count());

        IEnumerable<HeikinAshiResult> r1 = onequote.GetHeikinAshi();
        Assert.AreEqual(1, r1.Count());
    }
}
