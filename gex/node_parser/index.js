
import fs from "fs";
import { DemoParser } from "sdfz-demo-parser";

const args = process.argv;
if (args.length != 4) {
    console.error(`expected 2 arguments!\n<path to .sdfz file> <output file>\ninput: ${args.join(" ")}`);
    process.exit();
}

(async () => {
    const parser = new DemoParser();

    parser.onPacket.add(p => {
        //console.log(JSON.stringify(p, null, 4));
    });

    console.log(`opening ${args[2]} and writing to ${args[3]}`);
    const demo = await parser.parseDemo(args[2]);

    fs.writeFileSync(args[3], JSON.stringify(demo, null, 4));
})();