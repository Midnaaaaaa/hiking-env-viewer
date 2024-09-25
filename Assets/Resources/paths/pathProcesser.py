import argparse
import json



def process_file(input_path, output_path, min_point, max_point):

    with open(input_path, 'r') as f:

        json_string = f.read()

    parsed_json = json.loads(json_string)

    for feature in parsed_json['features']:
        # Accede al campo 'geometry' y luego a 'coordinates'
        coordinates = feature['geometry']['coordinates']

        for i in range(len(coordinates[0])):
            coordinates[0][i][0] = coordinates[0][i][0] - min_point[0] #min_point[0] is for X
            coordinates[0][i][1] = coordinates[0][i][1] - min_point[1] #min_point[0] is for Y

    with open(output_path, 'w') as f:
        json.dump(parsed_json, f)


def main():

    parser = argparse.ArgumentParser(description='This script processes the paths')

    # Agregar los argumentos
    parser.add_argument('-i', '--input', type=str, help='Input file path', required=True)
    parser.add_argument('-o','--output', type=str, help='Output file path')
    parser.add_argument('-cmX','--cropMinX', type=float, help='Minimum X point of the crop', required=True)
    parser.add_argument('-cmY','--cropMinY', type=float, help='Minimum Y point of the crop', required=True)
    parser.add_argument('-cMX','--cropMaxX', type=float, help='Maximum X point of the crop')
    parser.add_argument('-cMY','--cropMaxY', type=float, help='Maximum Y point of the crop')

    # Parsear los argumentos
    args = parser.parse_args()
    if args.output == None: args.output = "output.txt"

    process_file(args.input, args.output, (args.cropMinX, args.cropMinY), (args.cropMaxX, args.cropMaxY))


main()

